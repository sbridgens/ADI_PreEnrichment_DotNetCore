using System.Threading.Tasks;
using Application.Models;
using CSharpFunctionalExtensions;

namespace Application.DataAccess.Persistence.Contracts
{
    public interface IGraceNoteMetadataProvider
    {
        Task<Result<PackageEntry>> RetrieveAndAddProgramMapping(PackageEntry entry);
        Task<Result<PackageEntry>> RetrieveAndAddProgramData(PackageEntry entry);
        Task<Result<PackageEntry>> RetrieveAndAddSeriesSeasonSpecialsData(PackageEntry entry);
        Result UpdateGnMappingData(PackageEntry entry);
        Result SeedGnMappingData(PackageEntry entry);
    }
}