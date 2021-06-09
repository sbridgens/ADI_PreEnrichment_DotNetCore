using Application.Models;
using Domain.Entities;

namespace Application.Validation.Contracts
{
    public interface IPackageValidator
    {
        bool IsValidIngest(PackageEntry entry);
        bool IsValidUpdate(PackageEntry entry);
        bool IsValidForEnrichment(PackageEntry entry, Adi_Data dbAdiData);
    }
}