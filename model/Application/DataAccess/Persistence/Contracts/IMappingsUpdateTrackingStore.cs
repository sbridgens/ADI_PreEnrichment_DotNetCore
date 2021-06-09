using System;
using System.Collections.Generic;
using Domain.Entities;
using Domain.Schema.GNMappingSchema;

namespace Application.DataAccess.Persistence.Contracts
{
    public interface IMappingsUpdateTrackingStore
    {
        MappingsUpdateTracking GetTrackingItemByUid(Guid ingestUuid);

        MappingsUpdateTracking GetTrackingItemByPidPaid(string gnProviderId);

        List<MappingsUpdateTracking> GetPackagesRequiringEnrichment();

        long GetLastUpdateIdFromLatestUpdateIds();

        string GetLowestUpdateIdFromMappingTable();

        string GetLowestUpdateIdFromMappingTrackingTable();

        void UpdateMappingData(Guid uuid, GnOnApiProgramMappingSchema.onProgramMappingsProgramMapping mappingData,
            string nextUpdateId, string maxUpdateId);
    }
}