using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.BusinessLogic.Contracts;
using Application.Configuration;
using Application.DataAccess.Persistence.Contracts;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Options;

namespace Application.BusinessLogic
{
    public class TrackingProcessor : IGnTrackingOperations
    {
        private readonly ILogger<TrackingProcessor> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly IMappingsUpdateTrackingStore _mappingStore;
        private readonly ILayer1UpdateTrackingStore _layer1Store;
        private readonly ILayer2UpdateTrackingStore _layer2Store;
        private readonly GN_UpdateTracker_Config _options;
        private readonly IGnUpdatesProcessor _gnUpdatesProcessor;
        
        private long _lastLowestMappingId;
        private long _lastLowestLayer1Id;
        private long _lastLowestLayer2Id;

        public TrackingProcessor(ILogger<TrackingProcessor> logger, IApplicationDbContext dbContext, IGnUpdatesProcessor gnUpdatesProcessor,
            IMappingsUpdateTrackingStore mappingStore, ILayer1UpdateTrackingStore layer1Store, ILayer2UpdateTrackingStore layer2Store,
            IOptions<GN_UpdateTracker_Config> options)
        {
            _logger = logger;
            _dbContext = dbContext;
            _mappingStore = mappingStore;
            _layer1Store = layer1Store;
            _layer2Store = layer2Store;
            _options = options.Value;
            _gnUpdatesProcessor = gnUpdatesProcessor;
            _lastLowestMappingId = 0;
        }

        public async Task<Result> StartProcessing(CancellationToken token)
        {
            try
            {
                InUpdateOperation(true);
                await CheckAndProcessMappingUpdates();
                await CheckAndProcessProgramUpdates(1);
                await CheckAndProcessProgramUpdates(2);
                return Result.Success();
            }
            catch (Exception spex)
            {
                LogError("StartOperations", "Error during Processing", spex);
            }
            finally
            {
                InUpdateOperation(false);
            }

            return Result.Failure("Error during processing");
        }

        private void InUpdateOperation(bool inUpdateOperation)
        {
            var row = _dbContext.LatestUpdateIds.FirstOrDefault();
            if (row == null)
                return;
            row.InOperation = inUpdateOperation;
            _dbContext.LatestUpdateIds.Update(row);
            _dbContext.SaveChanges();
        }

        private async Task CheckAndProcessMappingUpdates()
        {
            try
            {
                var lowestUpdateId = GetLastLowestUpdateId();
                _logger.LogInformation($"Mapping UpdateId being used for Updates Call to Gracenote: {lowestUpdateId}");
                var mappingResult = await _gnUpdatesProcessor.GetGracenoteMappingData(lowestUpdateId.ToString(), _options.ApiMappingsLimit);
                if (mappingResult.IsFailure)
                {
                    _logger.LogError(mappingResult.Error);
                    return;
                }

                var nextId = long.Parse(mappingResult.Value.Mapping_NextUpdateId);
                var maxId = long.Parse(mappingResult.Value.Mapping_MaxUpdateId);
                
                CheckMaxUpdates(maxId, lowestUpdateId, "Mapping");
                UpdateLatestUpdateId(nextId, maxId);
                _logger.LogInformation("Mapping updates check successful");
            }
            catch (Exception capmuEx)
            {
                LogError("CheckAndProcessMappingUpdates", "Error during Parsing of Mapping Updates", capmuEx);
            }
        }

        private long GetLastLowestUpdateId()
        {
            if (_lastLowestMappingId != 0) return _lastLowestMappingId;
            
            _lastLowestMappingId = GetLayerUpdateId("Mapping", _mappingStore.GetLastUpdateIdFromLatestUpdateIds,
                _mappingStore.GetLowestUpdateIdFromMappingTrackingTable, _mappingStore.GetLowestUpdateIdFromMappingTable);
            if (_lastLowestMappingId >= 1) return _lastLowestMappingId;
            
            _logger.LogInformation($"No entry found in the LatestUpdateIds db table, adding new row with Mapping Update Id: {_lastLowestMappingId} " +
                                   "and 0 for the Layer1/2 column these will update during workflow.");
            var mapId = new LatestUpdateIds
            {
                LastMappingUpdateIdChecked = _lastLowestMappingId,
                LastLayer1UpdateIdChecked = 0,
                LastLayer2UpdateIdChecked = 0
            };
            _dbContext.LatestUpdateIds.Add(mapId);
            _dbContext.SaveChanges();
            return _lastLowestMappingId;
        }
 
        private void UpdateLatestUpdateId(long nextId, long maxId)
        {
            var layer1UpdateId = nextId != 0 ? nextId : maxId;
            var layer2UpdateId = nextId != 0 ? nextId : maxId;
            
            var updateTracker = _dbContext.LatestUpdateIds.SingleOrDefault();
            var updated = false;
            if (updateTracker == null)
            {
                _logger.LogError("Update latest update id is null, which should not be possible");
                return;
            }

            if (updateTracker.LastMappingUpdateIdChecked < nextId)
            {
                updateTracker.LastMappingUpdateIdChecked = nextId;
                updated = true;
            }

            if (updateTracker.LastLayer1UpdateIdChecked < layer1UpdateId)
            {
                updateTracker.LastLayer1UpdateIdChecked = layer1UpdateId;
                updated = true;
            }

            if (updateTracker.LastLayer2UpdateIdChecked < layer2UpdateId)
            {
                updateTracker.LastLayer2UpdateIdChecked = layer2UpdateId;
                updated = true;
            }

            if (!updated) return;
            _dbContext.LatestUpdateIds.Update(updateTracker);
            _dbContext.SaveChanges();
        }

        private async Task CheckAndProcessProgramUpdates(int layer)
        {
            try
            {
                var lowestUpdateId = GetLastLayerUpdateId(layer);
                _logger.LogInformation($"Layer{layer} UpdateId being used for Updates Call to Gracenote: {lowestUpdateId}");
                var updatesResult = await _gnUpdatesProcessor.GetGracenoteProgramUpdates(lowestUpdateId.ToString(), _options.ApiLayer1and2Limit, layer);
                if (updatesResult.IsFailure)
                {
                    _logger.LogError(updatesResult.Error);
                    return;
                }
                
                var nextId = long.Parse(updatesResult.Value.NextId);
                var maxId = long.Parse(updatesResult.Value.MaxId);
                CheckMaxUpdates(maxId, lowestUpdateId, $"Layer{layer}");
                _logger.LogInformation($"Number of Layer{layer} Programs requiring updates is: {updatesResult.Value.NumberOfPackages}");
                UpdateLatestUpdateId(nextId, maxId);
                _logger.LogInformation($"Layer{layer} updates check successful");
            }
            catch (Exception cappuex)
            {
                LogError("CheckAndProcessProgramUpdates", $"Error during Parsing of {layer} Updates", cappuex);
            }
        }

        private long GetLastLayerUpdateId(int layer)
        {
            var lastLayerUpdateId = layer == 1
                ? _lastLowestLayer1Id
                : _lastLowestLayer2Id;

            if (lastLayerUpdateId != 0) return lastLayerUpdateId;
            lastLayerUpdateId = layer == 1 
                ? GetLayerUpdateId("Layer1", _layer1Store.GetLastUpdateIdFromLatestUpdateIds,
                    _layer1Store.GetLowestUpdateIdFromLayer1UpdateTrackingTable,
                    _layer1Store.GetLowestUpdateIdFromMappingTrackingTable)
                : GetLayerUpdateId("Layer2", _layer2Store.GetLastUpdateIdFromLatestUpdateIds,
                    _layer2Store.GetLowestUpdateIdFromLayer2UpdateTrackingTable,
                    _layer2Store.GetLowestUpdateIdFromMappingTrackingTable);
            if (layer == 1)
                _lastLowestLayer1Id = lastLayerUpdateId;
            else
                _lastLowestLayer2Id = lastLayerUpdateId;
            
            return lastLayerUpdateId;
        }

        private long GetLayerUpdateId(string layerName, Func<long> getLastUpdate, Func<string> getLowestFallback, Func<string> getMappingFallback)
        {
            try
            {
                var updateId = getLastUpdate();
                if (updateId == 0)
                    updateId = Convert.ToInt64(getLowestFallback() ?? getMappingFallback());
                return updateId;
            }
            catch (Exception gmvex)
            {
                LogError("GetUpdateId", $"Error Encountered Obtaining lowest db {layerName} Update ID", gmvex);
                return 0;
            }
        }

        private void CheckMaxUpdates(long maxLevelId, long lowestUpdateId, string level)
        {
            try
            {
                if (lowestUpdateId != maxLevelId) return;
                _logger.LogInformation($"Workflow has reached the Maximum Gracenote {level} UpdateId: {maxLevelId}");
                _logger.LogInformation("Continuing to check if a next update id is available and if there are updates including in this id?");
            }
            catch (Exception cmuException)
            {
                LogError("CheckMaxUpdates", $"Error While carrying out Max Updates Check for level: {level} and updateId: {lowestUpdateId}", cmuException);
            }
        }

        private void LogError(string functionName, string message, Exception ex)
        {
            _logger.LogError($"[{functionName}] {message}: {ex.Message}");
            if (ex.InnerException != null)
                _logger.LogError($"[{functionName}] Inner Exception:" +
                                 $" {ex.InnerException.Message}");
        }
    }
}