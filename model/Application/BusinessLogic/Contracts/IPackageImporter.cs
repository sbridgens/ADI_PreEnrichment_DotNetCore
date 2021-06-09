using System.IO;
using Application.Models;
using CSharpFunctionalExtensions;

namespace Application.BusinessLogic.Contracts
{
    public interface IPackageImporter
    {
        Result<PackageEntry> TryImportPackage(FileInfo package);
    }
}