using System;
using System.Linq;
using Application.Models;
using Domain.Entities;
using Domain.Schema.GNProgramSchema;
using Domain.Schema.GNProgramSchema.Extensions;
using Infrastructure.BusinessManager;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Validation
{
    public class EnrichmentLayer2Validator : EnrichmentValidator
    {
        private static class Layer2Keys
        {
            public const string EpisodeName = "Episode_Name";
            public const string EpisodeOriginal = "Episode_Ordinal";
            
            public const string SeriesId = "Series_ID";
            public const string SeriesOrdinal = "Series_Ordinal";
            public const string SeriesName = "Series_Name";
            public const string SeriesNumberOfItems = "Series_NumberOfItems";
            public const string SeriesSummaryShort = "Series_Summary_Short";
            
            public const string Layer2RootId = "GN_Layer2_RootId";
            public const string Layer2SeriesId = "GN_Layer2_SeriesId";
            public const string Layer2TmsId = "GN_Layer2_TMSId";
            
            public const string ProductionYears = "Production_Years";
            
            public const string NumberOfItems = "Show_NumberOfItems";
            public const string ShowId = "Show_ID";
            public const string ShowName = "Show_Name";
            public const string SummaryShort = "Show_Summary_Short";
        }
        
        private readonly GraceNoteData _graceNoteData;
        private readonly Adi_Data _dbAdiData;
        private readonly GnApiProgramsSchema.programsProgram _movieEpisodeData;
        
        public EnrichmentLayer2Validator(PackageEntry packageEntry, Adi_Data dbAdiData, ILogger logger)
            : base(packageEntry, logger)
        {
            _graceNoteData = packageEntry.GraceNoteData;
            _movieEpisodeData = _graceNoteData.MovieEpisodeProgramData;
            _dbAdiData = dbAdiData;
        }

        public bool ValidateLayer()
        {
            return ValidateEpisodeData() 
                   || ValidateSeriesItems() 
                   || ValidateLayer2IdData()
                   || ValidateProductionYears()
                   || ValidateShowData() 
                   || ValidateProgramGenreData(2);
        }
        
        private bool ValidateEpisodeData()
        {
            if (_dbAdiData.TmsId != _graceNoteData.MovieEpisodeProgramData.TMSId)
            {
                return AssetRequiresEnrichment("TMSID", _dbAdiData.TmsId, _graceNoteData.MovieEpisodeProgramData.TMSId);
            }

            var apiEpisodeName = _movieEpisodeData.GetEpisodeTitle();
            if (CheckSingleValue(Layer2Keys.EpisodeName, apiEpisodeName)) return true;

            var apiEpisodeOriginal = _graceNoteData.MovieEpisodeProgramData.GetEpisodeOrdinalValue();
            return CheckSingleValue(Layer2Keys.EpisodeOriginal, apiEpisodeOriginal);
        }

        private bool ValidateSeriesItems()
        {
            var apiSeriesId = _movieEpisodeData.GetGnSeriesId();
            if (CheckSingleValue(Layer2Keys.SeriesId, apiSeriesId)) return true;

            var apiOrdinal = _movieEpisodeData.GetSeriesOrdinalValue();
            if (CheckSingleValue(Layer2Keys.SeriesOrdinal, apiOrdinal)) return true;

            var previousSeriesName = ReturnSingleAppDataValue(Layer2Keys.SeriesName);
            var apiSeriesName = $"Season {_movieEpisodeData.GetEpisodeSeason()}";
            if (previousSeriesName != null && previousSeriesName != apiSeriesName)
            {
                return AssetRequiresEnrichment(Layer2Keys.SeriesName, previousSeriesName, apiSeriesName);
            }

            var seasonInfo = _graceNoteData.GetSeasonInfo();
            if (!seasonInfo.Any()) return true;
            
            var seasonData = seasonInfo.FirstOrDefault(i => i.seasonId == _movieEpisodeData.GetSeasonId().ToString());
            var previousNumberOfItems = ReturnSingleAppDataValue(Layer2Keys.SeriesNumberOfItems);
            if (seasonData?.totalSeasonEpisodes != "0" && previousNumberOfItems != seasonData?.totalSeasonEpisodes)
            {
                return AssetRequiresEnrichment(Layer2Keys.SeriesNumberOfItems, previousNumberOfItems, seasonData?.totalSeasonEpisodes);
            }

            if (seasonData?.descriptions == null) return false;

            var previousSummary = ReturnSingleAppDataValue(Layer2Keys.SeriesSummaryShort);
            var descriptorData = seasonData.descriptions?.desc
                .FirstOrDefault(d => Convert.ToInt32(d.size) == 250 ||
                                     Convert.ToInt32(d.size) >= 100)
                ?.Value;
            if (previousSummary != descriptorData)
            {
                return AssetRequiresEnrichment(Layer2Keys.SeriesSummaryShort, previousSummary, descriptorData, "\r\n");
            }
            
            return false;
        }

        private bool ValidateLayer2IdData()
        {
            var apiRootId = _graceNoteData.ShowSeriesSeasonProgramData?.rootId;
            if (CheckSingleValue(Layer2Keys.Layer2RootId, apiRootId)) return true;
            
            var previousSeriesId = ReturnSingleAppDataValue(Layer2Keys.Layer2SeriesId);
            var apiSeriesId = _movieEpisodeData.GetSeriesId();
            if (!string.IsNullOrEmpty(previousSeriesId) && previousSeriesId != apiSeriesId)
            {
                return AssetRequiresEnrichment(Layer2Keys.Layer2SeriesId, previousSeriesId, apiSeriesId);
            }
            
            var previousTmsId = ReturnSingleAppDataValue(Layer2Keys.Layer2TmsId);
            var apiTmsId = _graceNoteData.ShowSeriesSeasonProgramData?.connectorId;
            if (!string.IsNullOrEmpty(previousTmsId) && previousTmsId != apiTmsId)
            {
                return AssetRequiresEnrichment(Layer2Keys.Layer2TmsId, previousTmsId, apiTmsId);
            }

            return false;
        }

        private bool ValidateProductionYears()
        {
            var sPremiere = ValidateYear(_graceNoteData.GetSeriesPremiere().Year.ToString())
                ? _graceNoteData.GetSeriesPremiere().Year.ToString()
                : _graceNoteData.GetSeasonPremiere()?.Year.ToString();

            var sFinale = ValidateYear(_graceNoteData.GetSeriesFinale().Year.ToString())
                ? _graceNoteData.GetSeriesFinale().Year.ToString()
                : _graceNoteData.GetSeasonFinale()?.Year.ToString();

            var productionYears = sFinale != null & sFinale?.Length == 4 ? $"{sPremiere}-{sFinale}" : sPremiere;
            return CheckSingleValue(Layer2Keys.ProductionYears, productionYears);
        }

        private bool ValidateYear(string year)
        {
            return year.Length == 4;
        }

        private bool ValidateShowData()
        {
            if (!_graceNoteData.ShowSeriesSeasonProgramData.seasons.Any()) return true;

            var apiNumberOfSeasons = _graceNoteData.GetNumberOfSeasons().ToString();
            if (CheckSingleValue(Layer2Keys.NumberOfItems, apiNumberOfSeasons)) return true;

            var apiShowId = _graceNoteData.GetShowId();
            if (CheckSingleValue(Layer2Keys.ShowId, apiShowId)) return true;

            var apiShowName = _graceNoteData.GetShowName();
            if (CheckSingleValue(Layer2Keys.ShowName, apiShowName)) return true;

            var previousSummaryShort = ReturnSingleAppDataValue(Layer2Keys.SummaryShort);
            var descriptors = MetadataMapper.CheckAndReturnDescriptionData(
                    _graceNoteData.ShowSeriesSeasonProgramData.descriptions, true);
            if (!string.IsNullOrEmpty(descriptors) && previousSummaryShort != descriptors)
            {
                return AssetRequiresEnrichment(Layer2Keys.SummaryShort, previousSummaryShort, descriptors, "\r\n");
            }
            return false;
        }
    }
}