using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Domain.Entities;

namespace Application.BusinessLogic.Contracts
{
    public interface IGnUpdatesProcessor
    {
        public Task<Result<MappingsUpdateTracking>> GetGracenoteMappingData(string dbUpdateId, string apiLimit);
        /// <summary>
        /// Returns number of program data updates required
        /// </summary>
        public Task<Result<(int NumberOfPackages, string NextId, string MaxId)>> GetGracenoteProgramUpdates(string dbUpdateId, string limit, int layer);
    }
}