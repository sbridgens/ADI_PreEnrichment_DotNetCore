using Application.Models;

namespace Application.DataAccess.Persistence.Contracts
{
    public interface IProgramTypeLookupStore
    {
        void SetProgramType(PackageEntry entry, bool isTrackerService = false);
    }
}