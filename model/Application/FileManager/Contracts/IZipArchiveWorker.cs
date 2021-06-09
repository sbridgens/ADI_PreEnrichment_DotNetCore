using System.IO;
using Application.Models;
using CSharpFunctionalExtensions;

namespace Application.FileManager.Contracts
{
    public interface IZipArchiveWorker
    {
        Maybe<FileInfo> ExtractAdiXml(FileInfo archive, string workingDirectory);
        ArchiveInfo ReadArchiveInfo(FileInfo archive);
        Result ExtractArchive(ArchiveInfo archive, bool isUpdate);
        Result CreateArchive(string sourceDirectory, string destinationArchive);
    }
}