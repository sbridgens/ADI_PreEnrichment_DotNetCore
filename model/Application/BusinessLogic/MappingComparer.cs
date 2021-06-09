using System;
using System.Linq;
using Application.BusinessLogic.Contracts;
using Application.DataAccess.Persistence.Contracts;
using Application.FileManager.Serialization;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using MappingSchema = Domain.Schema.GNMappingSchema.GnOnApiProgramMappingSchema;
using ProgramSchema = Domain.Schema.GNProgramSchema.GnApiProgramsSchema;

namespace Application.BusinessLogic
{
    public class MappingComparer : IGnMappingComparer
    {
        private readonly ILogger<MappingComparer> _logger;
        private readonly IGnMappingDataStore _gnMappingDataService;
        private readonly IApplicationDbContext _dbContext;
        private readonly IXmlSerializationManager _serializationManager;

        public MappingComparer(ILogger<MappingComparer> logger, IApplicationDbContext dbContext,
            IXmlSerializationManager serializationManager, IGnMappingDataStore gnMappingDataService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _serializationManager = serializationManager;
            _gnMappingDataService = gnMappingDataService;
        }
        
        public bool MappingDataChanged(Guid ingestUuid)
        {
            try
            {
                var apiMappingData = _dbContext.GN_Api_Lookup.FirstOrDefault(m => m.IngestUUID == ingestUuid);
                if (apiMappingData == null) return false;
                
                var mappingSchema = _serializationManager.Read<MappingSchema.on>(apiMappingData.GnMapData);
                if (mappingSchema.programMappings.programMapping.Length == 0) return false;
                
                var programMapping = GetGnMappingData(mappingSchema);
                if (programMapping == null) return false;
                
                var dbMappingData = _gnMappingDataService.ReturnMapData(ingestUuid);
                return CheckIdTypes(programMapping, dbMappingData) 
                       || CheckLinkTypes(programMapping, dbMappingData);
            }
            catch (Exception mdcException)
            {
                _logger.LogError("MappingDataChanged", $"Error Encountered While comparing mapping data for IngestUUID: {ingestUuid}.", mdcException);
                return false;
            }
        }
        
        private MappingSchema.onProgramMappingsProgramMapping GetGnMappingData(MappingSchema.on coreGnMappingData)
        {
            return coreGnMappingData.programMappings.programMapping.FirstOrDefault(m =>
                m.status == MappingSchema.onProgramMappingsProgramMappingStatus.Mapped);
        }

        private bool CheckIdTypes(MappingSchema.onProgramMappingsProgramMapping programMapping, GN_Mapping_Data dbEnrichedMappingData)
        {
            var newTmsId = programMapping.id.FirstOrDefault(t => t.type.ToLower() == "tmsid")?.Value;
            var newRootId = programMapping.id.FirstOrDefault(r => r.type.ToLower() == "rootid")?.Value;

            if (newTmsId != dbEnrichedMappingData.GN_TMSID)
            {
                _logger.LogDebug($"Mapping TMSID Mapping Data changed: Previous = {dbEnrichedMappingData.GN_TMSID}, Api Value = {newTmsId}");
                return true;
            }

            if (newRootId == dbEnrichedMappingData.GN_RootID) return false;
            _logger.LogDebug($"Mapping RootId Data changed: Previous = {dbEnrichedMappingData.GN_RootID}, Api Value = {newRootId}");
            return true;
        }

        private bool CheckLinkTypes(MappingSchema.onProgramMappingsProgramMapping programMapping, GN_Mapping_Data dbEnrichedMappingData)
        {
            var newProviderId = GetLinkValue(programMapping, "providerid");
            var newPaid = GetLinkValue(programMapping, "paid");
            var newPid = GetLinkValue(programMapping, "pid");
            
            if (newProviderId == dbEnrichedMappingData.GN_ProviderId && newPaid == dbEnrichedMappingData.GN_Paid &&
                newPid == dbEnrichedMappingData.GN_Pid) return false;
            
            _logger.LogDebug($"providerid, paid or pid Mapping Data changed: Previous ProviderId = {dbEnrichedMappingData.GN_ProviderId}, Api Value = {newProviderId}" +
                             $"\r\nPrevious Paid = {dbEnrichedMappingData.GN_Paid}, Api Value = {newPaid}" +
                             $"\r\nPrevious Pid = {dbEnrichedMappingData.GN_Pid}, Api Value = {newPid}");
            return true;
        }

        private string GetLinkValue(MappingSchema.onProgramMappingsProgramMapping programMapping, string key)
        {
            return programMapping.link.FirstOrDefault(p => p.idType.ToLower() == key)?.Value;
        }
    }
}