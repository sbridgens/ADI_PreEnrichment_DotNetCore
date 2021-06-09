using System.Collections.Generic;
using System.IO;
using Application.FileManager.Serialization;
using CSharpFunctionalExtensions;

namespace Application.FileManager.Contracts
{
    public interface IFileSystemWorker
    {
        Result CheckAndDeletePosterAssets(DirectoryInfo folder);
        IEnumerable<FileInfo> GetArchives(string path);
        string GetFileSize(string fileName);
        string GetFileHash(string fileName);
        string ReadAdiAsString(string adiFileName);
        void SaveAdiFile(DirectoryInfo directoryInfo, ADI adiFileContent);
        void MoveToFailedFolder(FileInfo file);
        void MoveToNonMappedFolder(FileInfo file);        
        Result DeliverEnrichedAsset(string deliveryPackage, bool isTvodPackage);
        void CreateFolder(string folderPath);
        void DeliverZipArchive(string zipFilePath);
        void RemoveFolder(string path);
        void DeleteFile(FileInfo fileInfo);
        void DeleteFile(string path);
    }
}