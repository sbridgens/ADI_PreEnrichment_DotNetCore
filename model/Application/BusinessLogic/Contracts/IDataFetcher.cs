using System.Threading.Tasks;
using Application.Models;
using CSharpFunctionalExtensions;
using Domain.Entities;

namespace Application.BusinessLogic.Contracts
{
    public interface IDataFetcher
    {
        public Task<Result<GN_Api_Lookup>> FetchLayer(GN_Mapping_Data dbEnrichedMappingData, int layer, string tmsId, PackageEntry packageEntry);
        public Task<Result> GetGracenoteMovieEpisodeData(GN_Api_Lookup apiProgramData, PackageEntry packageEntry,
            GN_Mapping_Data dbEnrichedMappingData);
        public Task<Result> GetSeriesSeasonSpecialsData(GN_Api_Lookup apiProgramData, PackageEntry packageEntry,
            GN_Mapping_Data dbEnrichedMappingData);
    }
}