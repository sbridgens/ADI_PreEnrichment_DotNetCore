using System;
using System.Linq;
using Application.DataAccess.Persistence.Contracts;
using Application.Validation.Contracts;
using Domain.Entities;
using Infrastructure.ApiManager.Serialization;
using MappingSchema = Domain.Schema.GNMappingSchema.GnOnApiProgramMappingSchema;
using ProgramSchema = Domain.Schema.GNProgramSchema.GnApiProgramsSchema;

namespace Infrastructure.Validation
{
    public class DatabaseExistenceChecker : IDatabaseExistenceChecker
    {
        private readonly IApplicationDbContext _dbContext;
        
        public DatabaseExistenceChecker(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void EnsureMappingUpdateExist(MappingSchema.onProgramMappingsProgramMapping programMapping, string providerId)
        {
            var existsInMappingTable = _dbContext.GN_Mapping_Data.FirstOrDefault(m => m.GN_ProviderId == providerId);
            if (existsInMappingTable == null) return;

            var apiXmlData = XmlApiSerializationHelper<MappingSchema.on>.SerializeObjectToString(programMapping, true);
            var apiData = _dbContext.GN_Api_Lookup.FirstOrDefault(a => a.IngestUUID == existsInMappingTable.IngestUUID);
            if (apiData == null)
            {
                apiData = new GN_Api_Lookup
                {
                    IngestUUID = existsInMappingTable.IngestUUID,
                    GN_TMSID = programMapping.id.FirstOrDefault(t => t.type.ToLower() == "tmsid")?.Value,
                    GnMapData = apiXmlData
                };
                _dbContext.GN_Api_Lookup.Add(apiData);
            }
            else
            {
                apiData.GnMapData = apiXmlData;
                _dbContext.GN_Api_Lookup.Update(apiData);
            }
            
            _dbContext.SaveChanges();
        }

        public void EnsureLayer1UpdateLookupExist(GN_Mapping_Data mapping, ProgramSchema.programsProgram programData)
        {
            var apiXmlData = XmlApiSerializationHelper<ProgramSchema.on>.SerializeObjectToString(programData, false);
            var apiData = _dbContext.GN_Api_Lookup.FirstOrDefault(a => a.IngestUUID == mapping.IngestUUID);
            if (apiData == null)
            {
                apiData = new GN_Api_Lookup
                {
                    IngestUUID = mapping.IngestUUID,
                    GN_TMSID = mapping.GN_TMSID,
                    GnLayer1Data = apiXmlData
                };
                _dbContext.GN_Api_Lookup.Add(apiData);
            }
            else
            {
                apiData.GnLayer1Data = apiXmlData;
                apiData.GN_TMSID = mapping.GN_TMSID;
                _dbContext.GN_Api_Lookup.Update(apiData);
            }

            _dbContext.SaveChanges();
        }

        public void EnsureLayer2UpdateLookupExist(Guid programGuid, string connectorId, ProgramSchema.programsProgram programData)
        {
            var apiXmlData = XmlApiSerializationHelper<ProgramSchema.on>.SerializeObjectToString(programData, false);
            var apiData = _dbContext.GN_Api_Lookup.FirstOrDefault(a => a.IngestUUID == programGuid);
            if (apiData == null)
            {
                apiData = new GN_Api_Lookup
                {
                    IngestUUID = programGuid,
                    GN_TMSID = programData.TMSId,
                    GN_connectorId = connectorId,
                    GnLayer2Data = apiXmlData
                };
                _dbContext.GN_Api_Lookup.Add(apiData);
            }
            else
            {
                apiData.GnLayer2Data = apiXmlData;
                apiData.GN_TMSID = programData.TMSId;
                apiData.GN_connectorId = connectorId;
                _dbContext.GN_Api_Lookup.Update(apiData);
            }

            _dbContext.SaveChanges();
        }
    }
}