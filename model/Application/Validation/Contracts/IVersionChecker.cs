using Application.Models;

namespace Application.Validation.Contracts
{
    public interface IVersionChecker
    {
        bool IsHigherVersion(PackageEntry entry, int? dbVersionMajor, int? dbVersionMinor, bool isTvod = false);
    }
}