using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Application.Configuration;
using Application.FileManager.Contracts;
using Application.FileManager.Serialization;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.FileManager
{
    public class FileSystemWorker : IFileSystemWorker
    {
        private readonly ISystemClock _systemClock;
        private readonly IXmlSerializationManager _serializer;
        private readonly ILogger<FileSystemWorker> _logger;
        private readonly EnrichmentSettings _options;
        private DirectoryInfo folder;

        public FileSystemWorker(ISystemClock systemClock,
            IXmlSerializationManager serializer,
            IOptions<EnrichmentSettings> options,
            ILogger<FileSystemWorker> logger)
        {
            _systemClock = systemClock;
            _serializer = serializer;
            _logger = logger;
            _options = options.Value;
        }

        public Result CheckAndDeletePosterAssets(DirectoryInfo folder)
        {
            var patterns = new[] {".jpg", ".jpeg", ".gif", ".png", ".bmp"};

            try
            {
                var files = folder.EnumerateFiles()
                    .Where(file => patterns.Any(file.Name.ToLower().EndsWith))
                    .ToList();

                if (!files.Any())
                {
                    Result.Success();
                }

                foreach (var f in files)
                {
                    f.Delete();
                }

                return Result.Success();
            }
            catch (Exception e)
            {
                var message = $"Failed to remove poster assets in folder {folder.FullName}";
                _logger.LogError(e, message);
                return Result.Failure(message);
            }
        }

        public IEnumerable<FileInfo> GetArchives(string path)
        {
            var folder = new DirectoryInfo(path);
            
            if (!folder.Exists)
            {
                _logger.LogInformation($"Folder doesn't exist, creating: {folder.FullName}");
                folder.Create();
            }

            return folder
                .EnumerateFiles()
                .Where(x => x.Extension.Equals(".zip"))
                .OrderByDescending(info => info.CreationTimeUtc);
        }

        public string GetFileSize(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return string.Empty;
            }

            var info = new FileInfo(fileName);

            //Log.Info($"Filesize retrieved for {fileName}: {fsizeInfo.Length}");
            return info.Length.ToString();
        }

        public string GetFileHash(string fileName)
        {
            using var hashAlg = MD5.Create();
            using var fileStream = File.OpenRead(fileName);
            fileStream.Position = 0;
            var hashBytes = hashAlg.ComputeHash(fileStream);
            fileStream.Close();
            return BitConverter.ToString(hashBytes).Replace("-", "");
        }

        public string ReadAdiAsString(string adiFileName)
        {
            //in place to workaround a utf issue on the database table.
            var xdoc = XDocument.Load(adiFileName);
            xdoc.Declaration = new XDeclaration("1.0", Encoding.Unicode.HeaderName, null);
            return xdoc.ToString();
        }

        public void SaveAdiFile(DirectoryInfo directoryInfo, ADI adiFileContent)
        {
            //because NFS mounts are case sensitive, and we don't want files like aDi.XmL to stick around
            var adi = directoryInfo.EnumerateFiles()
                .Where(x => x.Name.ToLowerInvariant().Equals("adi.xml"))
                .ToList();
            foreach (var fileInfo in adi.Where(fileInfo => fileInfo.Exists))
            {
                fileInfo.Delete();
            }

            var adiFile = CombinePath(directoryInfo.FullName, "ADI.xml");
            
            _serializer.Save<ADI>(adiFile, adiFileContent);
        }

        public void MoveToFailedFolder(FileInfo file)
        {
            var failedFolder = new DirectoryInfo(_options.FailedDirectory);
            MoveToFolder(file, failedFolder);
        }

        public void MoveToNonMappedFolder(FileInfo file)
        {
            var nonMappedFolder = new DirectoryInfo(_options.MoveNonMappedDirectory);
            if (file.DirectoryName != null && file.DirectoryName.Equals(nonMappedFolder.FullName))
            {
                return;
            }
            
            file.CreationTimeUtc = _systemClock.UtcNow.UtcDateTime;
            MoveToFolder(file, nonMappedFolder);
        }

        public Result DeliverEnrichedAsset(string deliveryPackage, bool isTvodPackage)
        {
            try
            {
                var fInfo = new FileInfo(deliveryPackage);

                var tmpPackage = isTvodPackage
                    ? CombinePath(_options.TVOD_Delivery_Directory, $"{fInfo.Name}.tmp")
                    : CombinePath(_options.IngestDirectory, $"{fInfo.Name}.tmp");

                var finalPackage = isTvodPackage
                    ? CombinePath(_options.TVOD_Delivery_Directory, fInfo.Name)
                    : CombinePath(_options.IngestDirectory, fInfo.Name);

                _logger.LogInformation($"Moving Temp Package to ingest: {deliveryPackage} to {tmpPackage}");
                var destinationFolder = Path.GetDirectoryName(tmpPackage);
                if (!Directory.Exists(destinationFolder))
                {
                    Directory.CreateDirectory(destinationFolder);
                }
                
                File.Move(deliveryPackage, tmpPackage);
                if (File.Exists(tmpPackage))
                {
                    _logger.LogInformation("Temp package successfully moved");
                    _logger.LogInformation($"Moving Temp Package: {tmpPackage} to Ingest package {finalPackage}");
                    File.Move(tmpPackage, finalPackage);
                    _logger.LogInformation("Ingest package Delivered successfully.");
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                var message = "Error Delivering Final Package";
                _logger.LogError(ex, message);
                return Result.Failure(message);
            }
        }

        public void CreateFolder(string folderPath)
        {
            folder = new DirectoryInfo(folderPath);
            if (folder.Exists) 
                RemoveFolder(folderPath);
            folder.Create();
        }

        public void DeliverZipArchive(string zipFilePath)
        {
            var deliveryPackage = CombinePath(_options.TVOD_Delivery_Directory, zipFilePath); // todo : change directory?
            if (File.Exists(deliveryPackage))
            {
                throw new Exception($"Delivery File {deliveryPackage} already exists, failing this delivery as its a duplicate?");
            }
            
            var temporaryPackage = CombinePath(_options.TempWorkingDirectory, zipFilePath);
            MoveFile(temporaryPackage, $"{deliveryPackage}.tmp");
            MoveFile($"{deliveryPackage}.tmp", deliveryPackage);
            _logger.LogInformation($"Package {temporaryPackage} Successfully Delivered.");
        }

        private void MoveFile(string filePath, string destinationFilePath)
        {
            _logger.LogInformation($"Moving Package {filePath} to {destinationFilePath}");
            File.Move(filePath, destinationFilePath);
            if (!File.Exists(destinationFilePath))
                throw new Exception($"Failed to move file {filePath} to {destinationFilePath}");
            
            _logger.LogInformation($"Successfully moved Package: {filePath} to {destinationFilePath}");
        }

        public void RemoveFolder(string path)
        {
            var folderToRemove = new DirectoryInfo(path);
            if (!folderToRemove.Exists)
            {
                return;
            }
            
            folderToRemove.Delete(recursive: true);
        }

        public void DeleteFile(FileInfo fileInfo)
        {
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }
        }
        
        public void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private void MoveToFolder(FileInfo file, DirectoryInfo destinationFolder)
        {
            try
            {
                if (!destinationFolder.Exists)
                {
                    destinationFolder.Create();
                }

                file.MoveTo(CombinePath(destinationFolder.FullName, file.Name));
            }
            catch(IOException ioException)
            {
                _logger.LogError(ioException, $"Exception trying to move file {file.FullName} to folder {destinationFolder.FullName}" +
                                              $"\r\n{ioException.Message}");
            }
        }

        private string CombinePath(string path1, string path2)
        {
            var combinedPath = Path.Combine(path1, path2);
            return combinedPath.Replace('\\', '/');
        }
    }
}