using System;
using System.Collections.Generic;
using Domain.Entities;
using Domain.Schema.GNProgramSchema;

namespace Application.DataAccess.Persistence.Contracts
{
    public interface ILayer1UpdateTrackingStore
    {
        void SetLayer1RequiresUpdate(Layer1UpdateTracking rowData, bool updateValue);

        Layer1UpdateTracking GetTrackingItemByUid(Guid ingestUuid);

        Layer1UpdateTracking GetTrackingItemByTmsId(string tmsId);

        List<Layer1UpdateTracking> GetTrackingItemByTmsIdAndRootId(string tmsId, string rootId);

        List<Layer1UpdateTracking> GetPackagesRequiringEnrichment();

        long GetLastUpdateIdFromLatestUpdateIds();

        string GetLowestUpdateIdFromLayer1UpdateTrackingTable();

        string GetLowestUpdateIdFromMappingTrackingTable();

        void UpdateLayer1Data(Guid uuid, GnApiProgramsSchema.programsProgram programData,
            string nextUpdateId, string maxUpdateId);
    }
}