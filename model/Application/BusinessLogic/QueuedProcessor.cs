using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.BusinessLogic.Contracts;
using Application.Configuration;
using Application.FileManager.Contracts;
using Application.Metrics;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.BusinessLogic
{
    public class QueuedProcessor : IQueuedProcessor
    {
        private readonly IFileSystemWorker _fileSystemWorker;
        private readonly ISystemClock _systemClock;
        private readonly ILogger<QueuedProcessor> _logger;
        private readonly EnrichmentSettings _options;
        private readonly IPackageProcessor _packageProcessor;

        public QueuedProcessor(IPackageProcessor packageProcessor,
            IFileSystemWorker fileSystemWorker,
            ISystemClock systemClock,
            IOptions<EnrichmentSettings> options,
            ILogger<QueuedProcessor> logger)
        {
            _packageProcessor = packageProcessor;
            _fileSystemWorker = fileSystemWorker;
            _systemClock = systemClock;
            _logger = logger;
            _options = options.Value;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            MetricsRegistry.PackagesInQueue.Set(_fileSystemWorker.GetArchives(_options.InputDirectory).LongCount());
            MetricsRegistry.NotMappedPackages.Set(_fileSystemWorker.GetArchives(_options.MoveNonMappedDirectory).LongCount());
            
            foreach (var package in _fileSystemWorker.GetArchives(_options.InputDirectory))
            {
                await _packageProcessor.StartAsync(package, cancellationToken);
                MetricsRegistry.PackagesInQueue.Dec();
            }
            
            _logger.LogInformation($"Finished processing packages in {_options.InputDirectory}.");
            _logger.LogInformation($"Processing packages in {_options.MoveNonMappedDirectory}:");
            foreach (var nonMapped in _fileSystemWorker.GetArchives(_options.MoveNonMappedDirectory))
            {
                await ProcessNonMapped(nonMapped, cancellationToken);
                MetricsRegistry.NotMappedPackages.Set(_fileSystemWorker.GetArchives(_options.MoveNonMappedDirectory).LongCount());
            }
            
            _logger.LogInformation($"Finished processing packages in {_options.MoveNonMappedDirectory}.");
        }

        private async Task ProcessNonMapped(FileInfo nonMapped, CancellationToken cancellationToken)
        {
            var now = _systemClock.UtcNow.UtcDateTime;
            var created = nonMapped.CreationTimeUtc;
            var retryMaxDays = _options.FailedToMap_Max_Retry_Days;
            
            if ((now - created).Days > retryMaxDays)
            {
                _fileSystemWorker.MoveToFailedFolder(nonMapped);
            }

            await _packageProcessor.StartAsync(nonMapped, cancellationToken);
        }
    }
}