using System;
using System.Linq;
using System.Threading.Tasks;
using Application.BusinessLogic.Contracts;
using Application.DataAccess.Persistence.Contracts;
using Application.FileManager.Serialization;
using Application.Models;
using CSharpFunctionalExtensions;
using Domain.Entities;
using Infrastructure.ApiManager.Serialization;
using Microsoft.Extensions.Logging;
using MappingSchema = Domain.Schema.GNMappingSchema.GnOnApiProgramMappingSchema;
using ProgramSchema = Domain.Schema.GNProgramSchema.GnApiProgramsSchema;

namespace Infrastructure.Validation
{
    public class DataFetcher : IDataFetcher
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IGraceNoteApi _graceNote;
        private readonly ILogger<DataFetcher> _logger;
        private readonly IXmlSerializationManager _serializationManager;
        private readonly IProgramTypeLookupStore _programTypeLookupStore;
        public DataFetcher(IGraceNoteApi graceNote,
            IApplicationDbContext dbContext, IXmlSerializationManager serializationManager, IProgramTypeLookupStore programTypeLookupStore,
            ILogger<DataFetcher> logger)
        {
            _dbContext = dbContext;
            _graceNote = graceNote;
            _logger = logger;
            _serializationManager = serializationManager;
            _programTypeLookupStore = programTypeLookupStore;
        }

        public async Task<Result<GN_Api_Lookup>> FetchLayer(GN_Mapping_Data dbEnrichedMappingData, int layer, string tmsId, PackageEntry packageEntry)
        {
            try
            {
                var apiData = GetGracenoteApi(dbEnrichedMappingData);
                var programData = await _graceNote.GetProgramData(tmsId);
                if (programData.IsFailure) return Result.Failure<GN_Api_Lookup>($"Failed to retrieve gracenote data for TmsId {tmsId}, layer {layer}");
                
                switch (layer)
                {
                    case 1 : 
                        ProcessLayerOne(packageEntry, programData.Value, apiData);
                        break;
                    case 2 : 
                        ProcessLayerTwo(packageEntry, programData.Value, apiData);
                        break;
                    default:
                        var errorMessage = "Argument should be equal to 1 or 2. Other values are forbidden.";
                        throw new ArgumentOutOfRangeException(nameof(layer), errorMessage);
                }
                
                _dbContext.GN_Api_Lookup.Update(apiData);
                _dbContext.SaveChanges();
                var apiProgramData = _dbContext.GN_Api_Lookup.FirstOrDefault(m => m.IngestUUID == dbEnrichedMappingData.IngestUUID);
                return Result.Success(apiProgramData);
            }
            catch (Exception ggpdEx)
            {
                _logger.LogError("[GetGraceNoteSeriesSeasonSpecialsData] Error obtaining " +
                              $"Gracenote Api data: {ggpdEx.Message}");
                if (ggpdEx.InnerException != null)
                    _logger.LogError("[GetGraceNoteSeriesSeasonSpecialsData] " +
                                  $"Inner exception: {ggpdEx.InnerException.Message}");
                return Result.Failure<GN_Api_Lookup>("Fetching layer failure");
            }
        }

        private GN_Api_Lookup GetGracenoteApi(GN_Mapping_Data dbEnrichedMappingData)
        {
            var apiData = _dbContext.GN_Api_Lookup.FirstOrDefault(a => a.IngestUUID == dbEnrichedMappingData.IngestUUID);
            if (apiData != null) return apiData;
            apiData = new GN_Api_Lookup
            {
                IngestUUID = dbEnrichedMappingData.IngestUUID,
                GN_TMSID = dbEnrichedMappingData.GN_TMSID
            };

            _dbContext.GN_Api_Lookup.Add(apiData);
            _dbContext.SaveChanges();
            return apiData;
        }

        private void ProcessLayerOne(PackageEntry packageEntry, ProgramSchema.on programData, GN_Api_Lookup apiData)
        {
            packageEntry.GraceNoteData.CoreProgramData = programData;
            var apiXmlData = XmlApiSerializationHelper<ProgramSchema.on>
                .SerializeObjectToString(packageEntry.GraceNoteData.CoreProgramData.programs.FirstOrDefault(), false);
            apiData.GnLayer1Data = apiXmlData;
        }

        private void ProcessLayerTwo(PackageEntry packageEntry, ProgramSchema.on programData, GN_Api_Lookup apiData)
        {
            packageEntry.GraceNoteData.CoreSeriesData = programData;
            var apiXmlData = XmlApiSerializationHelper<ProgramSchema.on>
                .SerializeObjectToString(packageEntry.GraceNoteData.CoreSeriesData.programs.FirstOrDefault(), false);
            apiData.GnLayer2Data = apiXmlData;
        }
        
        public async Task<Result> GetGracenoteMovieEpisodeData(GN_Api_Lookup apiProgramData, PackageEntry packageEntry,
            GN_Mapping_Data dbEnrichedMappingData)
        {
            try
            {
                if (apiProgramData.GnLayer1Data != null)
                {
                    packageEntry.GraceNoteData.CoreProgramData = _serializationManager.Read<ProgramSchema.on>(apiProgramData.GnLayer1Data);
                }
                else
                {
                    var programResult = await _graceNote.GetProgramData(dbEnrichedMappingData.GN_TMSID);
                    packageEntry.GraceNoteData.CoreProgramData = programResult.Value;
                }

                packageEntry.GraceNoteData.MovieEpisodeProgramData =
                    packageEntry.GraceNoteData.CoreProgramData.programs.FirstOrDefault();
                packageEntry.GraceNoteData.GraceNoteConnectorId = dbEnrichedMappingData.GN_connectorId;
                
                _programTypeLookupStore.SetProgramType(packageEntry, true);
                var seasonId = Convert.ToInt32(packageEntry.GraceNoteData.MovieEpisodeProgramData?.seasonId);
                var packageSeasonId = seasonId > 0 ? seasonId : 0;
                if (packageEntry.SeasonId == 0)
                    packageEntry.SeasonId = packageSeasonId;
                
                BuildListData(packageEntry);
                return Result.Success();
            }
            catch (Exception ex)
            {
                var errorMessage =
                    $"GetGracenoteMovieEpisodeData : Error Obtaining / Parsing GN Program Data -  {ex.Message}";
                _logger.LogError(errorMessage);
                return Result.Failure(errorMessage);
            }
        }
        
        private void BuildListData(PackageEntry packageEntry)
        {
            packageEntry.GraceNoteData.EnrichmentDataLists ??= new EnrichmentDataLists();
            var apiData = packageEntry.GraceNoteData.MovieEpisodeProgramData;
            packageEntry.GraceNoteData.EnrichmentDataLists.UpdateListData(apiData, packageEntry.SeasonId.ToString());
        }
        
        public async Task<Result> GetSeriesSeasonSpecialsData(GN_Api_Lookup apiProgramData, PackageEntry packageEntry,
            GN_Mapping_Data dbEnrichedMappingData)
        {
            if (apiProgramData.GnLayer2Data != null)
            {
                packageEntry.GraceNoteData.CoreSeriesData = _serializationManager.Read<ProgramSchema.on>(apiProgramData.GnLayer2Data);
            }
            else
            {
                var programResult = await _graceNote.GetProgramData(dbEnrichedMappingData.GN_connectorId);
                packageEntry.GraceNoteData.CoreSeriesData = programResult.Value;
            }
            //should be tested thoroughly
            packageEntry.GraceNoteData.ShowSeriesSeasonProgramData = packageEntry.GraceNoteData.CoreSeriesData.programs.FirstOrDefault();

            return SetInitialLayer2SeriesData(packageEntry)
                .OnSuccessTry(() =>
                {
                    packageEntry.GraceNoteData.EnrichmentDataLists.UpdateListData(
                        packageEntry.GraceNoteData.ShowSeriesSeasonProgramData,
                        packageEntry.SeasonId.ToString());
                });
        }

        private Result SetInitialLayer2SeriesData(PackageEntry entry)
        {
            var seriesData = entry.GraceNoteData.ShowSeriesSeasonProgramData;
            if(entry.SeasonId == 0)
                entry.SeasonId = Convert.ToInt32(seriesData?.seasonId ?? entry.GraceNoteData.MovieEpisodeProgramData?.seasonId);

            if (seriesData?.seasons == null || !seriesData.seasons.Any()) return Result.Success();
            var seasonIdString = seriesData
                .seasons?
                .FirstOrDefault(s => s.seasonId == entry.GraceNoteData.MovieEpisodeProgramData.seasonId)
                ?.seasonId;
            entry.SeasonId = Convert.ToInt32(seasonIdString);
            _logger.LogInformation($"Program contains Season data for season ID: {entry.SeasonId}");

            return Result.Success();
        }
    }
}