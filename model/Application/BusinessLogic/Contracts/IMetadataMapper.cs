using System;
using System.Collections.Generic;
using Application.Models;
using CSharpFunctionalExtensions;
using Domain.Schema.GNProgramSchema;

namespace Application.BusinessLogic.Contracts
{
    public interface IMetadataMapper
    {
        Result InsertActorData(EnrichmentDataLists source, AdiData target);
        Result InsertCrewData(EnrichmentDataLists source, AdiData target);
        Result InsertTitleData(EnrichmentDataLists source, AdiData target, bool isMoviePackage);
        Result InsertDescriptionData(GnApiProgramsSchema.programsProgramDescriptions descriptions, AdiData target);
        Result InsertGenreData(EnrichmentDataLists source, AdiData target);
        Result InsertYearData(DateTime airDate, GnApiProgramsSchema.programsProgramMovieInfo movieInfo, AdiData target);

        Result InsertIdmbData(List<GnApiProgramsSchema.externalLinksTypeExternalLink> externalLinks, AdiData target,
            bool hasMovieInfo);

        Result InsertSeriesLayerData(string connectorId, string seriesId, AdiData target);

        Result InsertShowData(string showId, string showName, int numberOfSeasons,
            GnApiProgramsSchema.programsProgramDescriptions descriptions, bool hasSeasonInfo, AdiData target);

        Result InsertSeriesGenreData(EnrichmentDataLists source, AdiData target);

        Result InsertSeriesData(string seriesId, string seriesOrdinalValue,
            List<GnApiProgramsSchema.programsProgramSeason> seasonInfo, int seasonId, string episodeSeason,
            AdiData target, string seriesPrefix = null);

        Result InsertProductionYears(DateTime? seriesPremiere, DateTime? seasonPremiere, DateTime? seriesFinale,
            DateTime? seasonFinale, AdiData target);

        Result InsertProgramLayerData(string tmsId, string programRootId, string showRootId, AdiData target);
        Result CheckAndAddBlockPlatformData(AdiData target);
        Result SetQamUpdateContent(AdiData adiData, bool isUpdate);
    }
}