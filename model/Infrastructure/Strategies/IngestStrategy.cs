using System;
using System.Threading.Tasks;
using Application.BusinessLogic.Contracts;
using Application.Configuration;
using Application.DataAccess.Persistence.Contracts;
using Application.Extensions;
using Application.FileManager.Contracts;
using Application.Models;
using Application.Validation.Contracts;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Strategies
{
    public class IngestStrategy : BaseStrategy
    {
        private readonly IApplicationDbContext _context;
        private readonly IFileSystemWorker _fileSystemWorker;
        private readonly IGraceNoteMetadataProvider _graceNote;
        private readonly IImageWorker _imageWorker;
        private readonly ILogger<IngestStrategy> _logger;
        private readonly IPackageValidator _packageValidator;
        private readonly EnrichmentSettings _options;

        public IngestStrategy(IApplicationDbContext context,
            IFileSystemWorker fileSystemWorker,
            IGraceNoteMetadataProvider graceNote,
            IZipArchiveWorker zipWorker,
            ISystemClock systemClock,
            IPackageValidator packageValidator,
            IMetadataMapper metadataMapper,
            IGnMappingDataStore gnMappingDataStore,
            IImageWorker imageWorker,
            ILoggerFactory loggerFactory,
            IOptions<EnrichmentSettings> options) : base(
            context,
            fileSystemWorker,
            graceNote,
            zipWorker,
            systemClock,
            metadataMapper,
            gnMappingDataStore,
            imageWorker,
            loggerFactory,
            options
            )
        {
            _context = context;
            _fileSystemWorker = fileSystemWorker;
            _graceNote = graceNote;
            _packageValidator = packageValidator;
            _imageWorker = imageWorker;
            _logger = loggerFactory.CreateLogger<IngestStrategy>();
            _options = options.Value;
        }

        public override async Task<Result> Execute(PackageEntry entry)
        {
            if (!_packageValidator.IsValidIngest(entry))
            {
                return Result.Failure(
                    $"Package with Paid: {entry.TitlPaIdValue} already exists in the database, failing Ingest.");
            }

            _logger.LogInformation($"Package with Paid: {entry.TitlPaIdValue} " +
                                   "confirmed as a unique package, continuing ingest operations.");
            entry.IngestUuid = Guid.NewGuid();
            _logger.LogInformation($"New package Identifier Generated: {entry.IngestUuid}");

            var preparePackageResult = await _graceNote.RetrieveAndAddProgramMapping(entry)
                .Bind(PreparePackageForIngest)
                .Bind(() => SaveAdiToDatabase(entry))
                .Bind(() => SetAdiMovieMetadata(entry))
                .BindIf(!entry.HasPreviewAssets, () => CheckAndAddPreviewData(entry));

            return await preparePackageResult.Bind(() => base.Execute(entry));
        }

        private Result PreparePackageForIngest(PackageEntry entry)
        {
            return _graceNote.SeedGnMappingData(entry)
                .Bind(() => ExtractPackageMedia(entry))
                .TapIf(entry.ArchiveInfo.ExtractedMovieAsset != null, () => SetMovieAsset(entry))
                .TapIf(entry.HasPreviewAssets, () => SetPreviewAsset(entry))
                .Bind(() => SetAdiPackageVars(entry));
        }
    }
}