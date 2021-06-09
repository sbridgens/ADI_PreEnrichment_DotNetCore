using System.Threading;
using System.Threading.Tasks;
using Application.BusinessLogic.Contracts;
using Application.Configuration;
using Application.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ADI.UpdateTracking.Service.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly EnrichmentSettings _options;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ISystemClock _systemClock;

        public Worker(IServiceScopeFactory scopeFactory, ISystemClock systemClock, ILogger<Worker> logger,
            IOptions<EnrichmentSettings> options)
        {
            _scopeFactory = scopeFactory;
            _systemClock = systemClock;
            _logger = logger;
            _options = options.Value;
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            return BackgroundProcessing(cancellationToken);
        }

        private async Task BackgroundProcessing(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Service is starting: {_systemClock.UtcNow}");
            while (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation($"Start processing: {_systemClock.UtcNow}");
                using var scope = _scopeFactory.CreateScope();
                var queuedProcessor = scope.ServiceProvider.GetRequiredService<IGnTrackingOperations>();
                var processResult = await queuedProcessor.StartProcessing(cancellationToken);
                if(processResult.IsSuccess) _logger.LogInformation("Successfully finished processing");
                await Task.Delay(_options.PollIntervalInSeconds * 1000, cancellationToken);
            }

            _logger.LogInformation($"Service is stopping: {_systemClock.UtcNow}");
        }
    }
}