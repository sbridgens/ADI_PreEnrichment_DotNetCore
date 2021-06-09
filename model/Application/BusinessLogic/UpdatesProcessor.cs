using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.BusinessLogic.Contracts;
using Application.DataAccess.Persistence.Contracts;
using Application.Validation.Contracts;
using CSharpFunctionalExtensions;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using MappingSchema = Domain.Schema.GNMappingSchema.GnOnApiProgramMappingSchema;
using ProgramSchema = Domain.Schema.GNProgramSchema.GnApiProgramsSchema;

namespace Application.BusinessLogic
{
    public class UpdatesProcessor : IGnUpdatesProcessor
    {
        private readonly ILogger<UpdatesProcessor> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly IGraceNoteApi _graceNoteApi;
        private readonly IMappingsUpdateTrackingStore _mappingsUpdateTrackingStore;
        private readonly ILayer1UpdateTrackingStore _layer1UpdateTrackingStore;
        private readonly ILayer2UpdateTrackingStore _layer2UpdateTrackingStore;
        private readonly IDatabaseExistenceChecker _dbExistenceChecker;
        private int _numberOfProgramDataUpdatesRequired;
        
        public UpdatesProcessor(IApplicationDbContext dbContext, IGraceNoteApi graceNoteApi, IMappingsUpdateTrackingStore mappingsUpdateTrackingStore,
            ILayer1UpdateTrackingStore layer1UpdateTrackingStore, ILayer2UpdateTrackingStore layer2UpdateTrackingStore,
            ILogger<UpdatesProcessor> logger, IDatabaseExistenceChecker dbExistenceChecker)
        {
            _logger = logger;
            _dbContext = dbContext;
            _mappingsUpdateTrackingStore = mappingsUpdateTrackingStore;
            _graceNoteApi = graceNoteApi;
            _layer1UpdateTrackingStore = layer1UpdateTrackingStore;
            _layer2UpdateTrackingStore = layer2UpdateTrackingStore;
            _dbExistenceChecker = dbExistenceChecker;
        }

        public async Task<Result<MappingsUpdateTracking>> GetGracenoteMappingData(string dbUpdateId, string apiLimit)
        {
            try
            {
                var mappingsUpdate = new MappingsUpdateTracking();
                var numberOfMappingRequiringUpdate = 0;
                var gracenoteMappingData = await _graceNoteApi.GetProgramMappingsUpdatesData(dbUpdateId, apiLimit);
                if (gracenoteMappingData.Value == null) return Result.Failure<MappingsUpdateTracking>($"No Mapping Updates for UpdateId: {dbUpdateId}");
                var header = gracenoteMappingData.Value.header;
                mappingsUpdate.Mapping_MaxUpdateId = header.streamData.maxUpdateId.ToString();
                mappingsUpdate.Mapping_NextUpdateId = header.streamData.nextUpdateId.ToString();
                _logger.LogInformation($"Max Update ID: {mappingsUpdate.Mapping_MaxUpdateId}");
                _logger.LogInformation($"Next Update ID: {mappingsUpdate.Mapping_NextUpdateId}");

                if (header.streamData.nextUpdateId == 0)
                {
                    //No next id so we reached the max.
                    _logger.LogInformation("Workflow for Mapping updates has reached the Maximum Update Id, " +
                                       $"Setting Next updateid to Maximum Id: {mappingsUpdate.Mapping_MaxUpdateId}");
                    mappingsUpdate.Mapping_NextUpdateId = mappingsUpdate.Mapping_MaxUpdateId;
                }
                
                //Parse the mapping results and keep only the items that relate to the current ingest platform
                //only valid pid paid values starting with TITL belong to the current platform.
                var updatedMappingsData = GetUpdatedMappingsData(gracenoteMappingData.Value);
                //check if any of the filtered results are currently in the db if so they likely require an update
                foreach (var programMapping in updatedMappingsData)
                {
                    //obtain the provider id value for checks on the db
                    var providerId = programMapping.link
                        .FirstOrDefault(p => p.idType.ToLower().Equals("providerid"))?.Value;
                    if (string.IsNullOrEmpty(providerId)) continue;
                    _dbExistenceChecker.EnsureMappingUpdateExist(programMapping, providerId);
                    var existsInTracker = _mappingsUpdateTrackingStore.GetTrackingItemByPidPaid(providerId);
                    if (existsInTracker == null) continue;
                    _logger.LogInformation($"Mapping PIDPAID: {providerId} EXISTS IN THE DB Requires Update, Update id: {programMapping.updateId}");
                    //update the the counter for programs requiring adi updates
                    numberOfMappingRequiringUpdate++;
                    _logger.LogInformation($"Updating MappingsUpdateTracking Table with new mapping data for IngestUUID: {existsInTracker.IngestUUID}" +
                                       $" and PIDPAID: {existsInTracker.GN_ProviderId}");
                    //set the tracker service to flag the related asset as requiring an update.
                    //this flag will be used to trigger the adi creation service to generate a valid update against the correct ingestuuid.
                    //Sets the update ids too
                    _mappingsUpdateTrackingStore.UpdateMappingData(existsInTracker.IngestUUID, programMapping, 
                        mappingsUpdate.Mapping_NextUpdateId, mappingsUpdate.Mapping_MaxUpdateId);
                }

                //mappings requiring updates finished being calculated and can now be used to generate adi updates
                _logger.LogInformation($"Number of mappings requiring updates is: {numberOfMappingRequiringUpdate}");
                return Result.Success(mappingsUpdate);
            }
            catch (Exception ggmdex)
            {
                var errorMessage = "Error During Parsing of GetGracenote Mapping Data";
                LogError("GetGracenoteMappingData",
                    errorMessage, ggmdex);
                return Result.Failure<MappingsUpdateTracking>(errorMessage);
            }
        }

        private IEnumerable<MappingSchema.onProgramMappingsProgramMapping> GetUpdatedMappingsData(MappingSchema.on gracenoteMappingData)
        {
            return  (from mapping in gracenoteMappingData.programMappings.programMapping
                where mapping.status == MappingSchema.onProgramMappingsProgramMappingStatus.Mapped
                let paid = mapping.link.FirstOrDefault(t => t.idType.ToLower().Equals("paid"))
                where paid != null
                where paid.Value.ToLower().StartsWith("titl")
                select mapping).ToList();
        }

        public async Task<Result<(int NumberOfPackages, string NextId, string MaxId)>> GetGracenoteProgramUpdates(string dbUpdateId, string limit, int layer)
        {
            var layer1Update = new Layer1UpdateTracking();
            var layer2Update = new Layer2UpdateTracking();
            try
            {
                _numberOfProgramDataUpdatesRequired = 0;
                var entities = await _graceNoteApi.GetProgramsUpdatesData(dbUpdateId, limit);
                if (entities.Value == null) return Result.Failure<(int, string, string)>($"Package Layer{layer} data is Null cannot process package!");
                var maxId = entities.Value.header.streamData.maxUpdateId;
                var nextId = entities.Value.header.streamData.nextUpdateId;
                _logger.LogInformation($"Max Update ID for Layer{layer}: {maxId}");
                _logger.LogInformation($"Next Update ID for Layer{layer}: {nextId}");
                List<ProgramSchema.programsProgram> programDataList;
                switch (layer)
                {
                    case 1:
                        layer1Update.Layer1_MaxUpdateId = maxId.ToString();
                        layer1Update.Layer1_NextUpdateId = nextId.ToString();
                        
                        if (nextId == 0)
                        {
                            _logger.LogInformation($"Workflow for Layer{layer} updates has reached the Maximum Update Id, Setting Next updateid to Maximum Id: {maxId}");
                            layer1Update.Layer1_NextUpdateId = maxId.ToString();
                        }
                        
                        programDataList = (from programs in entities.Value.programs
                            let tmsId = programs.TMSId
                            where tmsId != null
                            select programs).ToList();
                        break;
                    case 2:
                        layer2Update.Layer2_MaxUpdateId = maxId.ToString();
                        layer2Update.Layer2_NextUpdateId = nextId.ToString();
                        if (nextId == 0)
                        {
                            _logger.LogInformation($"Workflow for Layer{layer} updates has reached the Maximum Update Id, Setting Next updateid to Maximum Id: {maxId}");
                            layer2Update.Layer2_NextUpdateId = maxId.ToString();
                        }
                        
                        programDataList = (from programs in entities.Value.programs
                                let connectorId = programs.connectorId
                                where connectorId != null
                                select programs).ToList();
                        break;
                    default: return Result.Failure<(int, string, string)>($"Layer {layer} cannot be present.");
                }

                foreach (var programData in programDataList)
                {
                    switch (layer)
                    {
                        case 1:
                            ParseLayer1Updates(programData, layer1Update);
                            break;
                        case 2:
                            ParseLayer2Updates(programData, layer2Update);
                            break;
                    }
                }
            }
            catch (Exception ggl1Uex)
            {
                LogError("GetGracenoteLayer1Updates",
                    $"Error During Parsing of GetGracenote Layer{layer} Data", ggl1Uex);
            }

            return Result.Success(GetOutput());
            
            (int, string, string) GetOutput()
            {
                var nextId = layer == 1
                    ? layer1Update.Layer1_NextUpdateId
                    : layer2Update.Layer2_NextUpdateId;
                var maxId = layer == 1
                    ? layer1Update.Layer1_MaxUpdateId
                    : layer2Update.Layer2_MaxUpdateId;
                return (_numberOfProgramDataUpdatesRequired, nextId, maxId);
            }
        }
        
        private void ParseLayer1Updates(ProgramSchema.programsProgram programData, Layer1UpdateTracking currentUpdate)
        {
            ValidateLayer1ExistsInDb(programData);
            var programExistsInDb =
                _layer1UpdateTrackingStore.GetTrackingItemByTmsIdAndRootId(programData.TMSId, programData.rootId);
            if (programExistsInDb == null) return;
            _logger.LogInformation($"Layer1 TMSID: {programData.TMSId} with RootId: {programData.rootId} EXISTS IN THE DB Requires Update, Update id: {programData.updateId}");
            _numberOfProgramDataUpdatesRequired++;
            foreach (var row in programExistsInDb)
            {
                _logger.LogInformation($"Updating Layer1UpdateTracking Table with new Layer1 data for IngestUUID: { row.IngestUUID} and TmsID: {row.GN_TMSID}");
                _layer1UpdateTrackingStore.UpdateLayer1Data(row.IngestUUID, programData, currentUpdate.Layer1_NextUpdateId, currentUpdate.Layer1_MaxUpdateId);
            }
        }

        private void ValidateLayer1ExistsInDb(ProgramSchema.programsProgram programData)
        {
            var mappings = _dbContext.GN_Mapping_Data
                .Where(m => m.GN_TMSID == programData.TMSId && m.GN_RootID == programData.rootId)
                .ToList();
            if (!mappings.Any()) return;
            foreach (var mapping in mappings)
            {
                _dbExistenceChecker.EnsureLayer1UpdateLookupExist(mapping, programData);
            }
        }
        
        private void ParseLayer2Updates(ProgramSchema.programsProgram programData, Layer2UpdateTracking currentUpdate)
        {
            if (programData.TMSId != programData.connectorId) return;
            
            ValidateLayer2ExistsInDb(programData);
            var programExistsInDb = _layer2UpdateTrackingStore
                .GetTrackingItemByConnectorIdAndRootId(programData.connectorId, programData.rootId);
            if (programExistsInDb == null) return;
            
            _logger.LogInformation($"Layer2 ConnectorId: {programData.connectorId} with RootId: {programData.rootId} EXISTS IN THE DB Requires Update, Update id: {programData.updateId}");
            _numberOfProgramDataUpdatesRequired++;
            foreach (var row in programExistsInDb)
            {
                _logger.LogInformation($"Updating Layer2UpdateTracking Table with new Layer2 data for IngestUUID: { row.IngestUUID} and ConnectorId: {row.GN_connectorId}");
                _layer2UpdateTrackingStore.UpdateLayer2Data(row.IngestUUID, programData, currentUpdate.Layer2_NextUpdateId, currentUpdate.Layer2_MaxUpdateId);
                _dbExistenceChecker.EnsureLayer2UpdateLookupExist(row.IngestUUID, row.GN_connectorId, programData);
            }
        }

        private void ValidateLayer2ExistsInDb(ProgramSchema.programsProgram programData)
        {
            var mappings = _dbContext.GN_Mapping_Data
                .Where(m => m.GN_connectorId == programData.connectorId && m.GN_RootID == programData.rootId)
                .ToList();
            if (!mappings.Any()) return;
            foreach (var mapping in mappings)
            {
                _dbExistenceChecker.EnsureLayer2UpdateLookupExist(mapping.IngestUUID, mapping.GN_connectorId, programData);
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