using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using MappingSchema = Domain.Schema.GNMappingSchema.GnOnApiProgramMappingSchema;
using ProgramSchema = Domain.Schema.GNProgramSchema.GnApiProgramsSchema;

namespace Application.DataAccess.Persistence.Contracts
{
    public interface IGraceNoteApi
    {
        Task<Result<MappingSchema.@on>> GetMappingData(string providerId);
        Task<Result<ProgramSchema.@on>> GetProgramData(string tmsId);
        Task<Result<MappingSchema.@on>> GetProgramMappingsUpdatesData(string updateId, string resultLimit);
        Task<Result<ProgramSchema.@on>> GetProgramsUpdatesData(string updateId, string resultLimit);
    }
}