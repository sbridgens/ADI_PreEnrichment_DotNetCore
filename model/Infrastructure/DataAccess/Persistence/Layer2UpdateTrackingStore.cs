using System;
using System.Collections.Generic;
using System.Linq;
using Application.DataAccess.Persistence.Contracts;
using Domain.Entities;
using Domain.Schema.GNProgramSchema;

namespace Infrastructure.DataAccess.Persistence
{
    public class Layer2UpdateTrackingStore : ILayer2UpdateTrackingStore
    {
        private readonly IApplicationDbContext _dbContext;
        
        public Layer2UpdateTrackingStore(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void SetLayer2RequiresUpdate(Layer2UpdateTracking rowData, bool updateValue)
        {
            rowData.RequiresEnrichment = updateValue;
            _dbContext.Layer2UpdateTracking.Update(rowData);
            _dbContext.SaveChanges();
        }

        public Layer2UpdateTracking GetTrackingItemByUid(Guid ingestUuid)
        {
            throw new NotImplementedException();
        }

        public Layer2UpdateTracking GetTrackingItemByConnectorId(string connectorId)
        {
            throw new NotImplementedException();
        }

        public List<Layer2UpdateTracking> GetTrackingItemByConnectorIdAndRootId(string connectorId, string rootId)
        {
            var rowData = _dbContext.Layer2UpdateTracking.Where(t => t.GN_connectorId == connectorId &
                                       t.Layer2_RootId == rootId &
                                       t.RequiresEnrichment == false).ToList();
            if (rowData.Count == 0) return null;
            //check if any l1 data with the same uid requires enrichment
            var rowUpdate = rowData.Select(layer2 => _dbContext.Layer1UpdateTracking
                .FirstOrDefault(layer1 => layer1.IngestUUID == layer2.IngestUUID));
            var hasL1Update = rowUpdate.Any(layer1 => layer1 != null && layer1.RequiresEnrichment);
            if (hasL1Update) return null;

            foreach (var row in rowData)
            {
                if (row.RequiresEnrichment) continue;
                var mapData = _dbContext.MappingsUpdateTracking.FirstOrDefault(m =>
                    m.IngestUUID == row.IngestUUID);
                if (mapData?.RequiresEnrichment != false) continue;
                SetLayer2RequiresUpdate(row, true);
                //only return rowdata for items not requiring enrichment in the previous tables
                return rowData;
            }

            return null;
        }

        public List<Layer2UpdateTracking> GetPackagesRequiringEnrichment()
        {
            return _dbContext.Layer2UpdateTracking.Where(p => p.RequiresEnrichment).ToList();
        }

        public string GetLowestUpdateIdFromLayer2UpdateTrackingTable()
        {
            var minVal = _dbContext.Layer2UpdateTracking.OrderBy(u => u.Layer2_UpdateId).First();
            return minVal.Layer2_UpdateId ?? "0";
        }

        public string GetLowestUpdateIdFromMappingTrackingTable()
        {
            var minVal = _dbContext.MappingsUpdateTracking.OrderBy(u => u.Mapping_UpdateId).First();
            return minVal.Mapping_UpdateId;
        }

        public long GetLastUpdateIdFromLatestUpdateIds()
        {
            var val = _dbContext.LatestUpdateIds.FirstOrDefault();
            return val?.LastLayer2UpdateIdChecked ?? 0;
        }

        public void UpdateLayer2Data(Guid uuid, GnApiProgramsSchema.programsProgram programData, string nextUpdateId, string maxUpdateId)
        {
            var rowData = _dbContext.Layer2UpdateTracking.FirstOrDefault(l2 => l2.IngestUUID == uuid);
            if (rowData == null) return;
            rowData.Layer2_UpdateId = programData.updateId;
            rowData.Layer2_UpdateDate = programData.updateDate;
            rowData.Layer2_NextUpdateId = nextUpdateId;
            rowData.Layer2_MaxUpdateId = maxUpdateId;
            rowData.UpdatesChecked = DateTime.Now;
            rowData.RequiresEnrichment = true;
            _dbContext.Layer2UpdateTracking.Update(rowData);
            _dbContext.SaveChanges();
        }
    }
}