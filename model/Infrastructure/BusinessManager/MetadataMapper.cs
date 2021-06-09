using System;
using System.Collections.Generic;
using System.Linq;
using Application.BusinessLogic.Contracts;
using Application.Configuration;
using Application.FileManager.Serialization;
using Application.Models;
using CSharpFunctionalExtensions;
using Domain.Schema.GNProgramSchema;
using Infrastructure.ApiManager.EqualityComparers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.BusinessManager
{
    public class MetadataMapper : IMetadataMapper
    {
        private readonly ILogger<MetadataMapper> _logger;
        private readonly EnrichmentSettings _options;

        public MetadataMapper(IOptions<EnrichmentSettings> options, ILogger<MetadataMapper> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public Result InsertActorData(EnrichmentDataLists source, AdiData target)
        {
            try
            {
                if (source.CastMembers != null)
                {
                    var counter = 0;
                    var actorsDisplay = "";

                    foreach (var member in source.CastMembers.Distinct(new CastComparer())
                        .ToList()
                        .Where(member => member.role.Equals("Actor") ||
                                         member.role.Equals("Voice")))
                    {
                        if (counter == 5)
                        {
                            target.AddTitleMetadataApp_DataNode("Actors_Display",
                                actorsDisplay.TrimEnd(','));
                            break;
                        }

                        var actorName = $"{member.name.first} {member.name.last}";
                        target.AddTitleMetadataApp_DataNode("Actors", actorName);
                        actorsDisplay += $"{actorName},";
                        counter++;
                    }

                    _logger.LogInformation("Actors data successfully added.");
                    return Result.Success();
                }

                _logger.LogWarning("No Actors data available.");
                return Result.Success();
            }
            catch (Exception iadEx)
            {
                _logger.LogError("[InsertActorData] Error Setting Actor data" +
                                 $": {iadEx.Message}");
                return Result.Failure(iadEx.Message);
            }
        }

        public Result InsertCrewData(EnrichmentDataLists source, AdiData target)
        {
            try
            {
                var producer = 0;
                var crewAdded = false;
                if (source.CrewMembers != null)
                {
                    foreach (var member in source.CrewMembers.Distinct(new CrewComparer()).ToList())
                    {
                        var memberName = $"{member.name.first} {member.name.last}";
                        switch (member.role)
                        {
                            case "Director":
                                target.AddTitleMetadataApp_DataNode("Director", memberName);
                                crewAdded = true;
                                break;
                            case "Producer" when producer < 2:
                            case "Executive Producer" when producer < 2:
                                target.AddTitleMetadataApp_DataNode("Producer", memberName);
                                crewAdded = true;
                                producer++;
                                break;
                            case "Writer":
                            case "Screenwriter":
                                target.AddTitleMetadataApp_DataNode("Writer", memberName);
                                crewAdded = true;
                                break;
                        }
                    }
                }

                if (crewAdded)
                {
                    _logger.LogInformation("Crew data successfully added");
                }
                else
                {
                    _logger.LogWarning("No crew data found?");
                }

                //non mandatory
                return Result.Success();
            }
            catch (Exception icdEx)
            {
                _logger.LogError("[InsertCrewData] Error Setting Crew data" +
                                 $": {icdEx.Message}");

                return Result.Failure(icdEx.Message);
            }
        }

        public Result InsertTitleData(EnrichmentDataLists source, AdiData target, bool isMoviePackage)
        {
            try
            {
                var title = source.ProgramTitles.Where(t => t.type == "full")
                    .Select(t => t.Value)
                    .FirstOrDefault();

                var titleAdded = target.AddTitleMetadataApp_DataNode("Title", title);

                if (isMoviePackage)
                {
                    return Result.Success();
                }

                var sortTitle = source.ProgramTitles.FirstOrDefault(t => t.type == "sort")?.Value;

                if (string.IsNullOrEmpty(sortTitle))
                {
                    return Result.Success();
                }

                _logger.LogInformation("Title contains sort data, adding Show_Title_Sort_Name to ADI.");
                titleAdded = target.AddTitleMetadataApp_DataNode("Show_Title_Sort_Name", sortTitle);


                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("[InsertTitleData] Error Setting Title data" +
                                 $": {ex.Message}");

                if (ex.InnerException != null)
                {
                    _logger.LogError("[InsertTitleData] Inner Exception: " +
                                     $"{ex.InnerException.Message}");
                }

                return Result.Failure(ex.Message);
            }
        }

        public Result InsertDescriptionData(GnApiProgramsSchema.programsProgramDescriptions descriptions,
            AdiData target)
        {
            try
            {
                var desc = CheckAndReturnDescriptionData(descriptions);

                if (!string.IsNullOrEmpty(desc))
                {
                    var result = target.AddTitleMetadataApp_DataNode("Summary_Short", desc);
                    _logger.LogInformation("Description Data successfully added");
                    return Result.Success();
                }

                _logger.LogWarning("No description Data added");
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("[InsertDescriptionData] Error Setting Description data" +
                                 $": {ex.Message}");
                return Result.Failure(ex.Message);
            }
        }

        public Result InsertGenreData(EnrichmentDataLists source, AdiData target)
        {
            try
            {
                var currentId = "-9";

                foreach (var genre in source.GenresList.Distinct(new GenreComparer()).ToList())
                {
                    if (currentId != genre.genreId)
                    {
                        target.AddTitleMetadataApp_DataNode("Genre", genre.Value);
                        target.AddTitleMetadataApp_DataNode("GenreID", genre.genreId);
                    }

                    currentId = genre.genreId;
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("[InsertGenreData] Error Setting Genre data" +
                                 $": {ex.Message}");

                return Result.Failure(ex.Message);
            }
        }

        public Result InsertYearData(DateTime airDate, GnApiProgramsSchema.programsProgramMovieInfo movieInfo,
            AdiData target)
        {
            try
            {
                var yearData = GetYearData(airDate, movieInfo);
                if (!string.IsNullOrEmpty(yearData))
                {
                    var nodeToRemove =
                        target.Adi.Asset.Metadata.App_Data.FirstOrDefault(n =>
                            n.Name.ToLower().Equals("year"));
                    target.Adi.Asset.Metadata.App_Data.Remove(nodeToRemove);

                    target.AddTitleMetadataApp_DataNode("Year", yearData);
                }
                else
                {
                    _logger.LogWarning("No Year data found in API, Updated Year data will be omitted.");
                }

                //return true as non mandatory
                return Result.Success();
                ;
            }
            catch (Exception ex)
            {
                _logger.LogError("[InsertYearData] Error Setting Year data" +
                                 $": {ex.Message}");

                return Result.Failure(ex.Message);
                ;
            }
        }

        public Result InsertIdmbData(List<GnApiProgramsSchema.externalLinksTypeExternalLink> externalLinks,
            AdiData target, bool hasMovieInfo)
        {
            try
            {
                if (externalLinks.Count <= 0)
                {
                    return Result.Success();
                }

                if (!externalLinks.Any())
                {
                    return Result.Success();
                }

                var links = externalLinks;
                _logger.LogInformation("Adding IMDb_ID data.");
                target.AddTitleMetadataApp_DataNode("IMDb_ID", links.FirstOrDefault()?.id);

                if (!hasMovieInfo)
                {
                    _logger.LogInformation("Adding Show_IMDb_ID data.");
                    target.AddTitleMetadataApp_DataNode("Show_IMDb_ID",
                        links.Any()
                            ? links.Last().id
                            : links.FirstOrDefault()?.id);
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("[InsertIdmbData] Error Setting IDMB Data: " +
                                 $": {ex.Message}");

                return Result.Failure(ex.Message);
            }
        }

        public Result InsertSeriesLayerData(string connectorId, string seriesId, AdiData target)
        {
            return target.AddTitleMetadataApp_DataNode("GN_Layer2_TMSId", connectorId)
                .Bind(() => target.AddTitleMetadataApp_DataNode("GN_Layer2_SeriesId", seriesId));
        }

        public Result InsertShowData(string showId, string showName, int numberOfSeasons,
            GnApiProgramsSchema.programsProgramDescriptions descriptions,
            bool hasSeasonInfo,
            AdiData target)
        {
            var result = Result.Success();
            if (hasSeasonInfo)
            {
                result.Bind(() =>
                    target.AddTitleMetadataApp_DataNode("Show_NumberOfItems", numberOfSeasons.ToString()));
            }

            result.Bind(() => target.AddTitleMetadataApp_DataNode("Show_ID", showId))
                .Bind(() => target.AddTitleMetadataApp_DataNode("Show_Name", showName));

            var description = CheckAndReturnDescriptionData(descriptions, true);
            if (!string.IsNullOrEmpty(description))
            {
                result.Bind(() => target.AddTitleMetadataApp_DataNode("Show_Summary_Short", description));
            }
            else
            {
                _logger.LogWarning(
                    "No description data found at Layer2 for Show_Summary_Short, enrichment will continue without Show_Summary_Short data.");
            }

            return result;
        }

        public Result InsertSeriesGenreData(EnrichmentDataLists source, AdiData target)
        {
            var result = Result.Success();
            var gId = "";
            var genres = source.GenresList.Distinct(new GenreComparer()).ToList();


            foreach (var genre in genres)
            {
                if (result.IsFailure)
                {
                    return result;
                }

                if (gId != genre.genreId)
                {
                    result.Bind(() => target.AddTitleMetadataApp_DataNode("Show_Genre", genre.Value))
                        .Bind(() => target.AddTitleMetadataApp_DataNode("Show_GenreID", genre.genreId));
                }

                gId = genre.genreId;
            }

            return result;
        }

        public Result InsertSeriesData(string seriesId, string seriesOrdinalValue,
            List<GnApiProgramsSchema.programsProgramSeason> seasonInfo, int seasonId, string episodeSeason,
            AdiData target, string seriesPrefix = null)
        {
            var seriesIdValue = !string.IsNullOrWhiteSpace(seriesPrefix)
                ? $"{seriesPrefix}{seriesId}"
                : seriesId;

            var result = target.AddTitleMetadataApp_DataNode("Series_ID", seriesIdValue)
                .Bind(() => target.AddTitleMetadataApp_DataNode("Series_Ordinal", seriesOrdinalValue));

            if (seasonId == 0)
            {
                return result;
            }

            result = result.Bind(() => target.AddTitleMetadataApp_DataNode("Series_Name", $"Season {episodeSeason}"));

            if (!seasonInfo.Any())
            {
                return result;
            }

            var seasonData = seasonInfo.FirstOrDefault(i => i.seasonId == seasonId.ToString());
            if (seasonData?.totalSeasonEpisodes != "0")
            {
                result = result.Bind(() => target.AddTitleMetadataApp_DataNode("Series_NumberOfItems",
                    seasonData?.totalSeasonEpisodes));
            }

            if (seasonData?.descriptions != null)
            {
                result = result.Bind(() => target.AddTitleMetadataApp_DataNode("Series_Summary_Short",
                    seasonData.descriptions?.desc
                        .FirstOrDefault(d => Convert.ToInt32(d.size) == 250 ||
                                             Convert.ToInt32(d.size) >= 100)
                        ?.Value));
            }
            else
            {
                _logger.LogWarning("Season Description data not available?");
            }

            return result;
        }

        public Result InsertProductionYears(DateTime? seriesPremiere, DateTime? seasonPremiere, DateTime? seriesFinale,
            DateTime? seasonFinale, AdiData target)
        {
            string productionYears;
            var sPremiere = seriesPremiere?.Year.ToString().Length == 4
                ? seriesPremiere?.Year.ToString()
                : seasonPremiere?.Year.ToString();

            if ((sPremiere != null) & (sPremiere?.Length != 4))
            {
                return Result.Success();
            }

            _logger.LogInformation($"Premiere year: {sPremiere}");

            var sFinale = seriesFinale?.Year.ToString().Length == 4
                ? seriesFinale?.Year.ToString()
                : seasonFinale?.Year.ToString();

            if ((sFinale != null) & (sFinale?.Length == 4))
            {
                _logger.LogInformation($"Finale year: {sFinale}");
                productionYears = $"{sPremiere}-{sFinale}";
            }
            else
            {
                productionYears = sPremiere;
                _logger.LogInformation("No Finale year, using Premiere year only");
            }

            return target.AddTitleMetadataApp_DataNode("Production_Years", productionYears);
        }

        public Result InsertProgramLayerData(string tmsId, string programRootId, string showRootId, AdiData target)
        {
            if (string.IsNullOrEmpty(showRootId))
            {
                showRootId = programRootId;
            }

            return target.AddTitleMetadataApp_DataNode("GN_Layer1_TMSId", tmsId)
                .Bind(() => target.AddTitleMetadataApp_DataNode("GN_Layer1_RootId", programRootId))
                .Bind(() => target.AddTitleMetadataApp_DataNode("GN_Layer2_RootId", showRootId));
        }


        public Result CheckAndAddBlockPlatformData(AdiData target)
        {
            try
            {
                var providerName = target.Adi.Metadata.AMS.Provider;
                var assetId =
                    target.Adi.Asset.Asset
                        .FirstOrDefault(a =>
                            a.Metadata.AMS.Asset_Class.Equals("movie"))
                        ?.Metadata.AMS.Asset_ID;

                var providerList = _options.Block_Platform.Providers
                    .Split(',')
                    .ToList();

                foreach (var provider in providerList
                    .Where(provider => !string.IsNullOrEmpty(provider) &&
                                       provider.ToLower().Trim() == providerName.ToLower().Trim()))
                {
                    _logger.LogInformation($"Provider: {provider} matches the Block list, " +
                                           "adding the Block_Platform entry with a value of: " +
                                           $"{_options.Block_Platform.BlockPlatformValue}" +
                                           " for this provider.");

                    target.AddAssetMetadataApp_DataNode(assetId,
                        "Block_Platform",
                        _options.Block_Platform.BlockPlatformValue);
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                var message = "[CheckAndAddBlockPlatformData] Error Setting Block platform data" +
                              $": {ex.Message}";
                _logger.LogError(ex, message);
                return Result.Failure(message);
            }
        }


        public Result SetQamUpdateContent(AdiData adiData, bool isUpdate)
        {
            try
            {
                if (isUpdate)
                {
                    return Result.Success();
                }

                var enrichedMovieAsset =
                    adiData.EnrichedAdi.Asset.Asset.FirstOrDefault(c =>
                        c.Metadata.AMS.Asset_Class == "movie");

                if (enrichedMovieAsset == null)
                {
                    throw new Exception("Error retrieving previously Enriched movie section for QAM Update.");
                }

                var adiMovie = adiData.EnrichedAdi.Asset.Asset.FirstOrDefault(c =>
                    c.Metadata.AMS.Asset_Class == "movie");

                if (adiMovie != null)
                {
                    adiMovie.Content = new ADIAssetAssetContent {Value = enrichedMovieAsset.Content.Value};
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                var message = $"Error Encountered Setting QAM Update content field: {ex.Message}";
                _logger.LogError(ex, message);
                return Result.Failure(message);
            }
        }

        public static string CheckAndReturnDescriptionData(
            GnApiProgramsSchema.programsProgramDescriptions programDescriptions, bool isSeason = false)
        {
            if (!programDescriptions.desc.Any())
            {
                return string.Empty;
            }

            if (!isSeason)
            {
                foreach (var desc in programDescriptions.desc.OrderByDescending(d => d.size).ThenBy(d => d.type))
                {
                    if (desc.type == "plot")
                    {
                        switch (desc.size)
                        {
                            case "250":
                                return desc.Value;
                            case "100":
                                return desc.Value;
                        }
                    }

                    if ((desc.type == "generic") & (desc.size == "100"))
                    {
                        return desc.Value;
                    }
                }
            }

            // season or default fallback if above are not found
            return programDescriptions.desc.OrderByDescending(d => d.size)
                .Where(d => d.size == "250" || d.size == "100")
                .Select(t => t.Value)
                .FirstOrDefault()
                ?.ToString();
        }

        private string GetYearData(DateTime airDate, GnApiProgramsSchema.programsProgramMovieInfo movieInfo)
        {
            var result = string.Empty;
            if (!string.IsNullOrEmpty(airDate.Year.ToString()))
            {
                result = airDate.Year.ToString().Length == 4
                    ? airDate.Year.ToString()
                    : movieInfo?.yearOfRelease?.Length == 4
                        ? movieInfo.yearOfRelease
                        : string.Empty;
            }

            return result;
        }
    }
}