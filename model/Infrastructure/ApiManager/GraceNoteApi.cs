using System;
using System.Threading.Tasks;
using Application.Configuration;
using Application.DataAccess.Persistence.Contracts;
using CSharpFunctionalExtensions;
using Infrastructure.ApiManager.Serialization;
using Infrastructure.DataAccess.GraceNoteApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MappingSchema = Domain.Schema.GNMappingSchema.GnOnApiProgramMappingSchema;
using ProgramsSchema = Domain.Schema.GNProgramSchema.GnApiProgramsSchema;

namespace Infrastructure.ApiManager
{
    public class GraceNoteApi : IGraceNoteApi
    {
        private readonly ILogger<GraceNoteApi> _logger;
        private readonly EnrichmentSettings _options;

        //TODO: Inject IHttpClientFactory
        public GraceNoteApi(IOptions<EnrichmentSettings> options, ILogger<GraceNoteApi> logger)
        {
            _logger = logger;
            _options = options.Value;
        }

        public Task<Result<MappingSchema.@on>> GetMappingData(string providerId)
        {
            try
            {
                var mapUrl = $"{_options.OnApi}ProgramMappings?" +
                             $"providerId={providerId}&" +
                             $"api_key={_options.ApiKey}";

                _logger.LogInformation($"Calling On API Mappings url with Provider Value: {providerId}");
                var webClient = new WebClientManager();
                var response = webClient.HttpGetRequest(mapUrl);
                _logger.LogDebug($"RECEIVED MAPPING DATA FROM GRACENOTE: \r\n{response}");
                if (response != null && webClient.SuccessfulWebRequest)
                {
                    var serializer = new XmlApiSerializationHelper<MappingSchema.@on>();
                    return Task.FromResult(Result.Success(serializer.Read(response)));
                }

                throw new Exception($"Gracenote Mapping Data: {response}, " +
                                    $"Failed Web request: {webClient.SuccessfulWebRequest}," +
                                    $"Web request response code: {webClient.RequestStatusCode}");
            }
            catch (Exception ggmdEx)
            {
                return Task.FromResult(Result.Failure<MappingSchema.@on>(
                    $"[GetGracenoteMappingData] Error obtaining Gracenote mapping data: {ggmdEx.Message}"));
            }
        }

        public Task<Result<ProgramsSchema.@on>> GetProgramData(string tmsId)
        {
            try
            {
                _logger.LogInformation($"Retrieving MetaData from On API using TMSId: {tmsId}");
                var programUrl = $"{_options.OnApi}Programs?" +
                                 $"tmsId={tmsId}&" +
                                 $"api_key={_options.ApiKey}";


                _logger.LogInformation($"Calling On API Programs url with TmsId Value: {tmsId}");
                //TODO: use IHttpClient 
                var webClient = new WebClientManager();
                var response = webClient.HttpGetRequest(programUrl);

                if ((response != null) & webClient.SuccessfulWebRequest)
                {
                    var serializer = new XmlApiSerializationHelper<ProgramsSchema.@on>();
                    var result = serializer.Read(response);
                    return Task.FromResult(Result.Success(result));
                }

                throw new Exception("Error during receive of GN Api Program data, " +
                                    $"Web request data: {webClient.SuccessfulWebRequest}," +
                                    $"Web request response code: {webClient.RequestStatusCode}");
            }
            catch (Exception ggpdEx)
            {
                if (ggpdEx.InnerException != null)
                {
                    _logger.LogError("[GetGracenoteProgramData] " +
                                     $"Inner exception: {ggpdEx.InnerException.Message}");
                }
                return Task.FromResult(Result.Failure<ProgramsSchema.@on>(
                    $"[GetGracenoteProgramData] Error obtaining Gracenote Program data: {ggpdEx.Message}"));
            }
        }

        public Task<Result<MappingSchema.on>> GetProgramMappingsUpdatesData(string updateId, string resultLimit)
        {
            try
            {
                //http://on-api.gracenote.com/v3/ProgramMappings?updateId=10938407543&limit=100&api_key=wgu7uhqcqyzspwxj28mxgy4b
                var updateUrl = $"{_options.OnApi}ProgramMappings?" +
                                $"updateId={updateId}" +
                                $"&limit={resultLimit}" +
                                $"&api_key={_options.ApiKey}";
                _logger.LogInformation($"Calling On API url with Update Value: {updateId} and Limit: {resultLimit}");
                var webClient = new WebClientManager();
                var response = webClient.HttpGetRequest(updateUrl);
                _logger.LogInformation("Successfully Called Gracenote OnApi");
                if (response != null && webClient.SuccessfulWebRequest)
                {
                    var serializer = new XmlApiSerializationHelper<MappingSchema.on>();
                    var result = serializer.Read(response);
                    return Task.FromResult(Result.Success(result));
                }

                throw new Exception($"Gracenote Mapping Update Data: {response}, " +
                                    $"Successful Web request: {webClient.SuccessfulWebRequest}," +
                                    $"Web request response code: {webClient.RequestStatusCode}");
            }
            catch (Exception ggnmuex)
            {
                if (ggnmuex.InnerException != null)
                    _logger.LogError($"[GetGracenoteProgramMappingsUpdatesData] Inner exception: {ggnmuex.InnerException.Message}");
                
                return Task.FromResult(Result.Failure<MappingSchema.@on>(
                    $"[GetGracenoteProgramMappingsUpdatesData] Error obtaining Gracenote Mapping Update data: {ggnmuex.Message}"));
            }
        }
        
        public Task<Result<ProgramsSchema.on>> GetProgramsUpdatesData(string updateId, string resultLimit)
        {
            try
            {
                var updateUrl = $"{_options.OnApi}Programs?" +
                                $"updateId={updateId}" +
                                $"&limit={resultLimit}" +
                                $"&api_key={_options.ApiKey}";
                _logger.LogInformation($"Calling On API url with Update Value: {updateId} and Limit: {resultLimit}");
                var webClient = new WebClientManager();
                var response = webClient.HttpGetRequest(updateUrl);
                _logger.LogInformation("Successfully Called Gracenote OnApi");
                if (response != null && webClient.SuccessfulWebRequest)
                {
                    var serializer = new XmlApiSerializationHelper<ProgramsSchema.on>();
                    var result = serializer.Read(response);
                    return Task.FromResult(Result.Success(result));
                }

                throw new Exception($"Gracenote Program Update Data: {response}, " +
                                    $"Successful Web request: {webClient.SuccessfulWebRequest}," +
                                    $"Web request response code: {webClient.RequestStatusCode}");
            }
            catch (Exception ggnmuex)
            {
                if (ggnmuex.InnerException != null)
                    _logger.LogError($"[GetGracenoteProgramsUpdatesData] Inner exception: {ggnmuex.InnerException.Message}");
                
                return Task.FromResult(Result.Failure<ProgramsSchema.@on>(
                    $"[GetGracenoteProgramsUpdatesData] Error obtaining Gracenote Program Update data: {ggnmuex.Message}"));
            }
        }
    }
}