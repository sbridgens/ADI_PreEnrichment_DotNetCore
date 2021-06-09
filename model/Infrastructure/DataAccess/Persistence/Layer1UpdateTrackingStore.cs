using System;
using System.Collections.Generic;
using System.Linq;
using Application.DataAccess.Persistence.Contracts;
using Domain.Entities;
using Domain.Schema.GNProgramSchema;

namespace Infrastructure.DataAccess.Persistence
{
    //todo : add logging to update tracking stores
    public class Layer1UpdateTrackingStore : ILayer1UpdateTrackingStore
    {
        private readonly IApplicationDbContext _dbContext;
        public Layer1UpdateTrackingStore(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        
        public void SetLayer1RequiresUpdate(Layer1UpdateTracking rowData, bool updateValue)
        {
            rowData.RequiresEnrichment = updateValue;
            _dbContext.Layer1UpdateTracking.Update(rowData);
            _dbContext.SaveChanges();
        }

        public Layer1UpdateTracking GetTrackingItemByUid(Guid ingestUuid)
        {
            throw new NotImplementedException();
        }

        public Layer1UpdateTracking GetTrackingItemByTmsId(string tmsId)
        {
            throw new NotImplementedException();
        }

        public List<Layer1UpdateTracking> GetTrackingItemByTmsIdAndRootId(string tmsId, string rootId)
        {
            var rowData = _dbContext.Layer1UpdateTracking.Where(t => t.GN_TMSID == tmsId &
                                                         t.Layer1_RootId == rootId &
                                                         t.RequiresEnrichment == false).ToList();

            if (rowData.Count == 0) return null;
            //check if any l2 data with the same uid requires enrichment
            var hasL2Update = rowData.Select(layer1 => _dbContext.Layer2UpdateTracking.
                    FirstOrDefault(layer2 => layer2.IngestUUID == layer1.IngestUUID)).
                Any(layer2Data => layer2Data != null && layer2Data.RequiresEnrichment);
            
            if (hasL2Update) return null;

            foreach (var row in rowData)
            {
                if (row.RequiresEnrichment) continue;

                var mapData = _dbContext.MappingsUpdateTracking.FirstOrDefault(m =>
                    m.IngestUUID == row.IngestUUID);

                if (mapData?.RequiresEnrichment != false) continue;
                SetLayer1RequiresUpdate(row, true);
                //only return rowdata for items not requiring enrichment in the previous tables
                return rowData;
            }

            return null;
        }

        public List<Layer1UpdateTracking> GetPackagesRequiringEnrichment()
        {
            return _dbContext.Layer1UpdateTracking.Where(r => r.RequiresEnrichment).ToList();
        }

        public long GetLastUpdateIdFromLatestUpdateIds()
        {
            var val = _dbContext.LatestUpdateIds.FirstOrDefault();
            return val?.LastLayer1UpdateIdChecked ?? 0;
        }

        public string GetLowestUpdateIdFromLayer1UpdateTrackingTable()
        {
            var minVal = _dbContext.Layer1UpdateTracking.OrderBy(u => u.Layer1_UpdateId).First();
            return minVal?.Layer1_UpdateId ?? "0";
        }

        public string GetLowestUpdateIdFromMappingTrackingTable()
        {
            var minVal = _dbContext.MappingsUpdateTracking.OrderBy(u => u.Mapping_UpdateId).First();
            return minVal.Mapping_UpdateId;
        }

        public void UpdateLayer1Data(Guid uuid, GnApiProgramsSchema.programsProgram programData, string nextUpdateId, string maxUpdateId)
        {
            var rowData = _dbContext.Layer1UpdateTracking.FirstOrDefault(l1 => l1.IngestUUID == uuid);
            if (rowData == null) return;
            rowData.Layer1_UpdateId = programData.updateId;
            rowData.Layer1_UpdateDate = programData.updateDate;
            rowData.Layer1_NextUpdateId = nextUpdateId;
            rowData.Layer1_MaxUpdateId = maxUpdateId;
            rowData.UpdatesChecked = DateTime.Now;
            rowData.RequiresEnrichment = true;
            _dbContext.Layer1UpdateTracking.Update(rowData);
            _dbContext.SaveChanges();
        }
    }
}