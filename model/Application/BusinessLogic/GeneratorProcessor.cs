using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.BusinessLogic.Contracts;
using Application.DataAccess.Persistence.Contracts;
using Application.Models;
using CSharpFunctionalExtensions;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.BusinessLogic
{
    public class GeneratorProcessor : IGnTrackingOperations
    {
        private readonly ILogger<GeneratorProcessor> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly IGnMappingComparer _mappingComparer;
        private readonly IGnLayerDataComprarer _gnLayerDataComparer;
        private readonly IWorkflowExecutor _workflowExecutor;
        
        public GeneratorProcessor(ILogger<GeneratorProcessor> logger, IApplicationDbContext dbContext, 
            IGnMappingComparer mappingComparer, IGnLayerDataComprarer gnLayerDataComparer, IWorkflowExecutor workflowExecutor)
        {
            _logger = logger;
            _dbContext = dbContext;
            _mappingComparer = mappingComparer;
            _gnLayerDataComparer = gnLayerDataComparer;
            _workflowExecutor = workflowExecutor;
        }

        public async Task<Result> StartProcessing(CancellationToken token)
        {
            var packageEntry = new PackageEntry();
            var canContinue =_dbContext.LatestUpdateIds.FirstOrDefault();
            if(canContinue != null && canContinue.InOperation)
            {
                var message = "Update operation is currently in progress waiting for next scheduled run.";
                _logger.LogWarning(message);
                return Result.Failure(message);
            }
            GetAndParseMappingUpdates(packageEntry);
            await GetAndParseLayer1Updates(packageEntry);
            await GetAndParseLayer2Updates(packageEntry);
            return Result.Success();
        }

        private void GetAndParseMappingUpdates(PackageEntry packageEntry)
        {
            var mappingUpdates = _dbContext.MappingsUpdateTracking.Where(r => r.RequiresEnrichment).ToList();
            foreach (var update in mappingUpdates)
            {
                ProcessMappingUpdate(packageEntry, update);
            }
            if(!mappingUpdates.Any())
                _logger.LogInformation("There are no mapping updates requiring enrichment");
        }

        private void ProcessMappingUpdate(PackageEntry packageEntry, MappingsUpdateTracking update)
        {
            var layerName = "Mapping";
            var comparingResult = _mappingComparer.MappingDataChanged(update.IngestUUID);
            if (!comparingResult)
            {
                update.RequiresEnrichment = false;
                _dbContext.MappingsUpdateTracking.Update(update);
                _dbContext.SaveChanges();
                return;
            }
                    
            var updateInfoString = $"Ingest UUID {update.IngestUUID} and ProviderId: {update.GN_ProviderId}";
            ProcessUpdate(packageEntry, layerName, update, updateInfoString,
                updateDatabaseTable: () =>
                {
                    _dbContext.MappingsUpdateTracking.Update(update);
                    _dbContext.SaveChanges();
                });
        }

        private async Task GetAndParseLayer1Updates(PackageEntry packageEntry)
        {
            try
            {
                var layer1Updates = _dbContext.Layer1UpdateTracking.Where(r => r.RequiresEnrichment).ToList();
                foreach (var update in layer1Updates)
                {
                    await ProcessLayer1Update(packageEntry, update);
                }
                if(!layer1Updates.Any())
                    _logger.LogInformation("There are no layer 1 updates requiring enrichment");
            }
            catch (Exception gapl1UException)
            {
                LogError(nameof(GetAndParseLayer1Updates), "Error during Processing", gapl1UException);
            }
        }

        private async Task ProcessLayer1Update(PackageEntry packageEntry, Layer1UpdateTracking update)
        {
            var layerName = "Layer1";
            var comparingResult = await _gnLayerDataComparer.ProgramDataChanged(packageEntry, update.IngestUUID, 1);
            if (!comparingResult)
            {
                update.RequiresEnrichment = false;
                _dbContext.Layer1UpdateTracking.Update(update);
                _dbContext.SaveChanges();
                return;
            }
                    
            var updateInfoString = $"Ingest UUID {update.IngestUUID}, PAID {update.GN_Paid} and TmsID: {update.GN_TMSID}";
            ProcessUpdate(packageEntry, layerName, update, updateInfoString,
                updateDatabaseTable: () =>
                {
                    _dbContext.Layer1UpdateTracking.Update(update);
                    _dbContext.SaveChanges();
                });
        }

        private async Task GetAndParseLayer2Updates(PackageEntry packageEntry)
        {
            try
            {
                var layer2Updates = _dbContext.Layer2UpdateTracking.Where(p => p.RequiresEnrichment).ToList();
                foreach (var update in layer2Updates)
                {
                    await ProcessLayer2Update(packageEntry, update);
                }
                if(!layer2Updates.Any())
                    _logger.LogInformation("There are no layer 2 updates requiring enrichment");
            }
            catch (Exception gapl2UException)
            {
                LogError(nameof(GetAndParseLayer2Updates), "Error during Processing", gapl2UException);
            }
        }

        private async Task ProcessLayer2Update(PackageEntry packageEntry, Layer2UpdateTracking update)
        {
            var layerName = "Layer2";
            var comparingResult = await _gnLayerDataComparer.ProgramDataChanged(packageEntry, update.IngestUUID, 2);
            if (!comparingResult)
            {
                update.RequiresEnrichment = false;
                _dbContext.Layer2UpdateTracking.Update(update);
                _dbContext.SaveChanges();
                return;
            }
                    
            var updateInfoString = $"Ingest UUID {update.IngestUUID}, PAID {update.GN_Paid} and ConnectorId: {update.GN_connectorId}";
            ProcessUpdate(packageEntry, layerName, update, updateInfoString,
                updateDatabaseTable: () =>
                {
                    _dbContext.Layer2UpdateTracking.Update(update);
                    _dbContext.SaveChanges();
                });
        }

        private void ProcessUpdate(PackageEntry packageEntry, string layerName, IUpdateTracking update, string updateInfoString, Action updateDatabaseTable)
        {
            try
            {
                _logger.LogInformation($"Processing {layerName} Requiring Update with {updateInfoString}");
                if (_workflowExecutor.Execute(packageEntry, update.IngestUUID).IsSuccess)
                {
                    _logger.LogInformation($"Update package created successfully, Updating {layerName} tracker table with process date/time and removing requires enrichment flag.");
                    update.UpdatesChecked = DateTime.Now;
                    update.RequiresEnrichment = false;
                    updateDatabaseTable();
                    _logger.LogInformation($"Successfully updated {layerName} Tracker table.");
                    _logger.LogInformation($"Processing FINISHED For {layerName} Update with {updateInfoString}");
                }
                else
                {
                    throw new Exception($"Processing{layerName} failed for update with {updateInfoString}, please check logs.");
                }
            }
            catch (Exception feex)
            {
                LogError($"{layerName} Update Foreach", $"Error Processing {layerName} Update Row Id: {update.Id}", feex);
                _workflowExecutor.Cleanup();
                _logger.LogError($"Processing {layerName} FAILED! For {layerName} Update with {updateInfoString}");
            }
        }
        
        private void LogError(string functionName, string message, Exception ex)
        {
            _logger.LogError($"[{functionName}] {message}: {ex.Message}");
            if (ex.InnerException == null) return;
            var theDeepestInnerException = ex.InnerException.InnerException ?? ex.InnerException;
            _logger.LogError($"[{functionName}] Inner Exception: {theDeepestInnerException.Message}");
        }
    }
}