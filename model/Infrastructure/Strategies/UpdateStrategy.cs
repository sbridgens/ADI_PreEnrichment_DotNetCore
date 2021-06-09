using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Application.BusinessLogic.Contracts;
using Application.Configuration;
using Application.DataAccess.Persistence.Contracts;
using Application.Extensions;
using Application.FileManager.Contracts;
using Application.FileManager.Serialization;
using Application.Models;
using Application.Validation.Contracts;
using CSharpFunctionalExtensions;
using Domain.Entities;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Strategies
{
    public class UpdateStrategy : BaseStrategy
    {
        private readonly IApplicationDbContext _context;
        private readonly IGraceNoteMetadataProvider _graceNote;
        private readonly ILogger<UpdateStrategy> _logger;
        private readonly IPackageValidator _packageValidator;
        private readonly IMetadataUpdater _metadataUpdater;
        private readonly IXmlSerializationManager _serializer;

        public UpdateStrategy(IApplicationDbContext context,
            IFileSystemWorker fileSystemWorker,
            IGraceNoteMetadataProvider graceNote,
            IZipArchiveWorker zipWorker,
            ISystemClock systemClock,
            IPackageValidator packageValidator,
            IMetadataMapper metadataMapper,
            IGnMappingDataStore gnMappingDataStore,
            IImageWorker imageWorker,
            IMetadataUpdater metadataUpdater,
            IXmlSerializationManager serializer,
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
            _graceNote = graceNote;
            _packageValidator = packageValidator;
            _metadataUpdater = metadataUpdater;
            _serializer = serializer;
            _logger = loggerFactory.CreateLogger<UpdateStrategy>();
        }

        public override async Task<Result> Execute(PackageEntry entry)
        {
            if (!_packageValidator.IsValidUpdate(entry))
            {
                return Result.Failure("Package is not a valid update package.");
            }

            var gnDataResult = await _graceNote.RetrieveAndAddProgramMapping(entry);
            if (gnDataResult.IsFailure)
            {
                return Result.Failure(gnDataResult.Error);
            }

            entry.AdiData.Entity = _context.Adi_Data.FirstOrDefault(i => i.IngestUUID == entry.IngestUuid);
            entry = gnDataResult.Value;
            return await _graceNote.UpdateGnMappingData(entry)
                .Bind(() => ExtractPackageMedia(entry))
                .TapIf(entry.ArchiveInfo.HasPreviewAssets, () => SetPreviewAsset(entry))
                .Bind(() => SetInitialUpdateData(entry))
                .Bind(() => base.Execute(entry));
        }

        private Result SetInitialUpdateData(PackageEntry packageEntry)
        {
            try
            {
                //Get the correct stored adi data
                var dbAdi = _context.Adi_Data.FirstOrDefault(i => i.IngestUUID.Equals(packageEntry.IngestUuid));
                if (dbAdi?.EnrichedAdi == null)
                {
                    throw new Exception($"Previously Enriched ADI data for Paid: " +
                                        $"{packageEntry.TitlPaIdValue} was not found in the database?");
                }

                return SerializeUpdateData(dbAdi, packageEntry.AdiData, packageEntry.GraceNoteData.GraceNoteTmsId,
                        packageEntry.IsTvodPackage)
                    //AdiContentController.RemoveMovieContentFromUpdate();
                    //Get original asset data and modify new adi.
                    .Bind(() => _metadataUpdater.CopyPreviouslyEnrichedAssetDataToAdi(packageEntry.AdiData, dbAdi.IngestUUID,
                        packageEntry.HasPreviewAssets, dbAdi.UpdateAdi != null))
                    .Bind(() => ProcessPackagePreviewData(packageEntry))
                    .OnSuccessTry(() =>
                    {
                        _context.Adi_Data.Update(dbAdi);
                        _context.SaveChanges();
                        _logger.LogInformation("Adi data updated in the database.");
                    }, exception =>
                    {
                        var message = "Error while updating Adi_Data in the database";
                        _logger.LogError(exception, message);
                        return message;
                    });
            }
            catch (Exception ex)
            {
                return _logger.LogErrorResult(ex, "Error Setting initial update data");
            }
        }

        private Result SerializeUpdateData(Adi_Data dbEntity, AdiData packageAdiData, string tmsId, bool isTvodPackage)
        {
            try
            {
                //Serialize previously enriched Adi File to obtain Asset data
                if (dbEntity.UpdateAdi != null)
                {
                    packageAdiData.UpdateAdi = _serializer.Read<ADI>(dbEntity.UpdateAdi);
                }
                else
                {
                    packageAdiData.EnrichedAdi = _serializer.Read<ADI>(dbEntity.EnrichedAdi);
                }

                packageAdiData.Entity.TmsId = tmsId;
                packageAdiData.Entity.VersionMajor = packageAdiData.GetVersionMajor();
                packageAdiData.Entity.VersionMinor = packageAdiData.GetVersionMinor();
                packageAdiData.Entity.Licensing_Window_End = packageAdiData.GetLicenceEndDate()
                    .ToString(CultureInfo.InvariantCulture);

                return Result.Success();
            }
            catch (Exception e)
            {
                return _logger.LogErrorResult(e, "Error while serializing update data.");
            }
        }

        private Result ProcessPackagePreviewData(PackageEntry entry)
        {
            if (!(entry.AdiData.HasPreviewMetadata() & entry.PreviewFileChecksum != null))
            {
                return Result.Success();
            }

            var previewAsset = entry.AdiData.Adi.Asset.Asset.FirstOrDefault(p =>
                p.Metadata.AMS.Asset_Class == "preview");

            if (previewAsset?.Metadata.App_Data == null)
            {
                return Result.Success();
            }

            foreach (var appdata in previewAsset.Metadata.App_Data)
            {
                appdata.Value = appdata.Name.ToLower() switch
                {
                    "content_checksum" => entry.PreviewFileChecksum,
                    "content_filesize" => entry.PreviewFileSize,
                    _ => appdata.Value
                };
            }

            return Result.Success();
        }
    }
}