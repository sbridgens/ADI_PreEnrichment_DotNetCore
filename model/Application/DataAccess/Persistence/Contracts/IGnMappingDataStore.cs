using System;
using System.Collections.Generic;
using CSharpFunctionalExtensions;
using Domain.Entities;
using Domain.Schema.GNProgramSchema;

namespace Application.DataAccess.Persistence.Contracts
{
    public interface IGnMappingDataStore
    {
        bool CleanMappingDataWithNoAdi();

        Result AddGraceNoteProgramData(Guid ingestGuid, string seriesTitle, string episodeTitle,
            GnApiProgramsSchema.programsProgram programDatas);

        Dictionary<string, string> ReturnDbImagesForAsset(string paidValue, int rowId, bool isTrackerService);

        GN_Mapping_Data ReturnMapData(Guid ingestGuid);
    }
}