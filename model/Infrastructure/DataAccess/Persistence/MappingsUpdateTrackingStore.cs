using System;
using System.Collections.Generic;
using System.Linq;
using Application.DataAccess.Persistence.Contracts;
using Domain.Entities;
using Domain.Schema.GNMappingSchema;

namespace Infrastructure.DataAccess.Persistence
{
    // todo : clear non-implemented methods
    public class MappingsUpdateTrackingStore : IMappingsUpdateTrackingStore
    {
        private readonly IApplicationDbContext _dbContext;
        public MappingsUpdateTrackingStore(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public MappingsUpdateTracking GetTrackingItemByUid(Guid ingestUuid)
        {
            throw new NotImplementedException();
        }

        public MappingsUpdateTracking GetTrackingItemByPidPaid(string gnProviderId)
        {
            var rowData = _dbContext.MappingsUpdateTracking
                .FirstOrDefault(t => t.RequiresEnrichment == false & t.GN_ProviderId == gnProviderId);
            if (rowData == null || rowData.RequiresEnrichment) return null;
            var layer1Data =
                _dbContext.Layer1UpdateTracking.FirstOrDefault(l1 => l1.IngestUUID == rowData.IngestUUID);

            var layer2Data =
                _dbContext.Layer2UpdateTracking.FirstOrDefault(l2 => l2.IngestUUID == rowData.IngestUUID);

            if (layer1Data?.RequiresEnrichment != false) return null;
            return layer2Data?.RequiresEnrichment != false ? null : rowData;
        }

        public List<MappingsUpdateTracking> GetPackagesRequiringEnrichment()
        {
            return _dbContext.MappingsUpdateTracking.Where(r => r.RequiresEnrichment).ToList();
        }

        public long GetLastUpdateIdFromLatestUpdateIds()
        {
            var val = _dbContext.LatestUpdateIds.FirstOrDefault();
            return val?.LastMappingUpdateIdChecked ?? 0;
        }

        public string GetLowestUpdateIdFromMappingTable()
        {
            var minVal = _dbContext.GN_Mapping_Data.OrderBy(u => u.GN_updateId).First();
            return minVal.GN_updateId;
        }

        public string GetLowestUpdateIdFromMappingTrackingTable()
        {
            var minVal = _dbContext.MappingsUpdateTracking.OrderBy(u => u.Mapping_UpdateId).First();
            return minVal.Mapping_UpdateId;
        }

        public void UpdateMappingData(Guid uuid, GnOnApiProgramMappingSchema.onProgramMappingsProgramMapping mappingData, string nextUpdateId, string maxUpdateId)
        {
            var rowData = _dbContext.MappingsUpdateTracking.FirstOrDefault(i => i.IngestUUID == uuid);
            if (rowData == null) return;
            rowData.Mapping_UpdateId = mappingData.updateId;
            rowData.Mapping_UpdateDate = mappingData.updateDate;
            rowData.Mapping_NextUpdateId = nextUpdateId;
            rowData.Mapping_MaxUpdateId = maxUpdateId;
            rowData.UpdatesChecked = DateTime.Now;
            rowData.RequiresEnrichment = true;
            _dbContext.MappingsUpdateTracking.Update(rowData);
            _dbContext.SaveChanges();
        }
    }
}