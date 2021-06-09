using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Application.BusinessLogic.Contracts;
using Application.Configuration;
using Application.FileManager.Contracts;
using Application.Metrics;
using Application.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.BusinessLogic
{
    public class AdiPackageProcessor : IPackageProcessor
    {
        private readonly IFileSystemWorker _fileSystemWorker;
        private readonly ILogger<AdiPackageProcessor> _logger;
        private readonly EnrichmentSettings _options;
        private readonly IPackageImporter _packageImporter;
        private readonly IStrategyFactory _strategyFactory;

        public AdiPackageProcessor(
            IStrategyFactory strategyFactory,
            IPackageImporter packageImporter,
            IFileSystemWorker fileSystemWorker,
            IOptions<EnrichmentSettings> options,
            ILogger<AdiPackageProcessor> logger)
        {
            _strategyFactory = strategyFactory;
            _packageImporter = packageImporter;
            _fileSystemWorker = fileSystemWorker;
            _options = options.Value;
            _logger = logger;
        }

        public async Task StartAsync(FileInfo package, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Processing package: {package.FullName}");
            var entryResult = _packageImporter.TryImportPackage(package);
            if (entryResult.IsFailure)
            {
                CleanupTempFolder(package);
                MetricsRegistry.PackagesFailedToProcess.Inc();
                _fileSystemWorker.MoveToFailedFolder(package);
                _logger.LogError(entryResult.Error);
                return;
            }

            var entry = entryResult.Value;
            if (entry.IsAdult && !_options.AllowAdultContentIngest.AllowAdultEnrichment)
            {
                ProcessFailure(entry,
                    $"Failed to process package with TitlPAID: {entry.TitlPaIdValue}. " +
                    "Processing is disabled for Adult packages.");
                return;
            }

            if (entry.IsUltraHd && !_options.ProcessUHDContent.AllowUHDContentIngest)
            {
                ProcessFailure(entry,
                    $"Failed to process package with TitlPAID: {entry.TitlPaIdValue}. " +
                    "Processing is disabled for UHD packages.");
                return;
            }

            var strategy = _strategyFactory.Get(entry);
            var result = await strategy.Execute(entry);
            if (result.IsFailure)
            {
                ProcessFailure(entry, result.Error);
                return;
            }

            ProcessSuccess(entry);
        }

        private void ProcessFailure(PackageEntry entry, string error)
        {
            _logger.LogError(error);
            CleanupTempFolder(entry.ArchiveInfo.Archive);
            if (entry.FailedToMap)
            {
                _fileSystemWorker.MoveToNonMappedFolder(entry.ArchiveInfo.Archive);
            }
            else
            {
                MetricsRegistry.PackagesFailedToProcess.Inc();
                _fileSystemWorker.MoveToFailedFolder(entry.ArchiveInfo.Archive);
            }
        }
        
        private void ProcessSuccess(PackageEntry entry)
        {
            MetricsRegistry.PackagesSuccessfullyProcessed.Inc();
            _logger.LogInformation($"FINISHED PROCESSING PACKAGE: {entry.ArchiveInfo.Archive.Name}");
            CleanupTempFolder(entry.ArchiveInfo.Archive);
            _fileSystemWorker.DeleteFile(entry.ArchiveInfo.Archive);
        }

        private void CleanupTempFolder(FileInfo package)
        {
            var tempFolder = Path.Combine(_options.TempWorkingDirectory, Path.GetFileNameWithoutExtension(package.Name));
            _fileSystemWorker.RemoveFolder(tempFolder);
        }
    }
}