using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Application.BusinessLogic.Contracts;
using Application.Configuration;
using Application.DataAccess.Persistence.Contracts;
using Application.Extensions;
using Application.FileManager.Contracts;
using Application.FileManager.Serialization;
using Application.Models;
using CSharpFunctionalExtensions;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.BusinessLogic
{
    public class WorkflowExecutor : IWorkflowExecutor
    {
        private static readonly List<string> AssetTypes = new List<string> {"movie", "preview"};
        
        private string _tempWorkingDirectory;
        private bool _workDirectoryCreated;
        
        private readonly ILogger<WorkflowExecutor> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly EnrichmentSettings _options;
        private readonly IFileSystemWorker _fileSystemWorker;
        private readonly IZipArchiveWorker _zipArchiveWorker;
        private readonly IXmlSerializationManager _serializationManager;

        public WorkflowExecutor(IOptions<EnrichmentSettings> options, ILogger<WorkflowExecutor> logger,
            IApplicationDbContext dbContext, IFileSystemWorker fileSystemWorker, IZipArchiveWorker zipArchiveWorker,
            IXmlSerializationManager serializationManager)
        {
            _logger = logger;
            _dbContext = dbContext;
            _options = options.Value;
            _fileSystemWorker = fileSystemWorker;
            _zipArchiveWorker = zipArchiveWorker;
            _serializationManager = serializationManager;
        }

        public Result Execute(PackageEntry packageEntry, Guid currentIngestUuid)
        {
            try
            {
                _workDirectoryCreated = false;
                _tempWorkingDirectory = "";
                
                var readResult = ReadAvailableAdiData(packageEntry, currentIngestUuid);
                if (readResult.IsFailure) return Result.Failure(readResult.Error);
                var updateAdi = readResult.Value;
                
                //Base template should be ready for packaging.
                //Package name convention = 001152118_20180527-1748 PAID + datetime
                var packageName = GetPackageName(updateAdi.TitlPaid);
                
                return CleanPackageAdiData(packageEntry)
                    .Bind(() => SetAdiAssetId(packageEntry, currentIngestUuid))
                    .Bind(() =>
                    {
                        _logger.LogInformation("Adi Data is ready for packaging");
                        return UpdateVersionMinor(packageEntry, updateAdi.IngestUUID);
                    })
                    .Bind(() => CreateWorkingDirectory(packageName))
                    .Bind(() => CreatePackageAndDeliver(packageEntry, packageName))
                    .Bind(Cleanup);
            }
            catch (Exception ewfException)
            {
                throw new Exception("Error during Execution of workflow functions", ewfException);
            }
        }

        private Result<Adi_Data> ReadAvailableAdiData(PackageEntry packageEntry, Guid currentIngestUuid)
        {
            var updateAdi = _dbContext.Adi_Data.FirstOrDefault(a => a.IngestUUID == currentIngestUuid);
            if (!string.IsNullOrEmpty(updateAdi?.UpdateAdi) || !string.IsNullOrEmpty(updateAdi?.EnrichedAdi))
            {
                var read = _serializationManager.Read<ADI>(updateAdi.UpdateAdi ?? updateAdi.EnrichedAdi);
                packageEntry.AdiData.UpdateAdi = read;
                _logger.LogInformation(
                    $"Successfully Loaded Update ADI File from DB where IngestId = {updateAdi.IngestUUID}");
            }
            else
            {
                var read = _serializationManager.Read<ADI>(updateAdi?.EnrichedAdi);
                packageEntry.AdiData.UpdateAdi = read;
                var errorMessage =
                    $"FAILED to Load Enriched ADI File from DB where IngestId = {updateAdi?.IngestUUID}, as Adi not present.";
                _logger.LogInformation(errorMessage);
                return Result.Failure<Adi_Data>(errorMessage);
            }

            return Result.Success<Adi_Data>(updateAdi);
        }

        private Result SetAdiAssetId(PackageEntry packageEntry, Guid currentIngestUuid)
        {
            var mapData = _dbContext.GN_Mapping_Data.FirstOrDefault(u => u.IngestUUID == currentIngestUuid);
            _logger.LogInformation($"Setting Asset_Id to the correct GN_Paid value: {mapData?.GN_Paid}");
            packageEntry.AdiData.UpdateAdi.Asset.Metadata.AMS.Asset_ID = mapData?.GN_Paid;
            return Result.Success();
        }

        private Result CleanPackageAdiData(PackageEntry packageEntry)
        {
            packageEntry.AdiData.RemoveDefaultAdiNodes();
            
            packageEntry.IsTvodPackage = IsTvodAsset(packageEntry.AdiData.UpdateAdi);
            _logger.LogInformation($"Is Update a TVOD Package: {packageEntry.IsTvodPackage}");
            
            _logger.LogInformation("Removing Image sections from ADI file");
            RemoveImageSectionsFromAdi(packageEntry);
            _logger.LogInformation("Image sections Successfully Removed from ADI file");

            var tvodMessage = packageEntry.IsTvodPackage
                ? "Removing Content Nodes from Move/Preview Sections for Tvod Asset"
                : "Removing Movie section from ADI file";
            _logger.LogInformation(tvodMessage);
            RemoveMovieSectionFromAdi(packageEntry);
            return Result.Success();
        }
        
        private bool IsTvodAsset(ADI entryAdi)
        {
            var first = entryAdi.Asset.Asset?.FirstOrDefault();

            if (first == null || !first.Metadata.AMS.Product.ToLower().Contains("tvod"))
            {
                return false;
            }

            _logger.LogInformation("Package Detected as a TVOD Asset.");
            return true;
        }

        private void RemoveImageSectionsFromAdi(PackageEntry packageEntry)
        {
            try
            {
                var assets = packageEntry.AdiData.UpdateAdi.Asset.Asset.ToList();
                foreach (var asset in from asset in assets
                    let img = asset.Metadata.AMS.Asset_Class == "image"
                    where img
                    select asset)
                {
                    packageEntry.AdiData.UpdateAdi.Asset.Asset.Remove(asset);
                }
            }
            catch (Exception risfaEx)
            {
                throw new Exception("Error encountered Removing Image section from ADI", risfaEx);
            }
        }
        
        private void RemoveMovieSectionFromAdi(PackageEntry packageEntry)
        {
            try
            {
                foreach (var item in AssetTypes)
                {
                    var assets = packageEntry.AdiData.UpdateAdi.Asset.Asset.ToList();
                    foreach (var asset in from asset in assets
                        let mov = asset.Metadata.AMS.Asset_Class == item
                        where mov
                        select asset)
                    {
                        if (!packageEntry.IsTvodPackage)
                        {
                            packageEntry.AdiData.UpdateAdi.Asset.Asset.Remove(asset);
                            _logger.LogInformation($"Successfully removed {item} section from Adi File");
                            continue;
                        }

                        _logger.LogInformation($"Removing TVOD Content Node from Adi File for {item}");
                        asset.Content = null;
                        _logger.LogInformation($"Successfully Removed Content tag from Adi File for {item}.");
                    }
                }
            }
            catch (Exception rmsfaEx)
            {
                throw new Exception("Error encountered Removing Movie section from ADI", rmsfaEx);
            }
        }
        
        private static string GetPackageName(string paid)
        {
            var dt = DateTime.Now.ToString("yyyyMMdd-HHmm");
            return $"{paid}_{dt}";
        }

        private Result UpdateVersionMinor(PackageEntry packageEntry, Guid ingestGuid)
        {
            var currentVersionMinor = GetAdiVersionMinor(ingestGuid);
            var newVersionMinor = currentVersionMinor + 1;
            _logger.LogInformation($"Updating Version Minor from {currentVersionMinor} to {newVersionMinor}");
            return UpdateAllVersionMinorValues(packageEntry, newVersionMinor);
        }

        private int GetAdiVersionMinor(Guid ingestGuid)
        {
            var versionMinor = _dbContext.Adi_Data.FirstOrDefault(i => i.IngestUUID == ingestGuid)?.VersionMinor;
            return versionMinor ?? 0;
        }

        private Result CreateWorkingDirectory(string packageName)
        {
            _tempWorkingDirectory = CombinePath(_options.TempWorkingDirectory, packageName);
            _logger.LogInformation($"Creating working directory: {_tempWorkingDirectory}");
            _fileSystemWorker.CreateFolder(_tempWorkingDirectory);
            _logger.LogInformation($"Working directory {_tempWorkingDirectory} created successfully");
            _logger.LogInformation($"Creating delivery directory: {_options.TVOD_Delivery_Directory}");
            _fileSystemWorker.CreateFolder(_options.TVOD_Delivery_Directory);
            _logger.LogInformation($"Delivery directory {_tempWorkingDirectory} created successfully");
            _workDirectoryCreated = true;
            return Result.Success();
        }

        private Result UpdateAllVersionMinorValues(PackageEntry packageEntry, int newVersionMinor)
        {
            try
            {
                packageEntry.AdiData.UpdateAdi.Metadata.AMS.Version_Minor = newVersionMinor;
                packageEntry.AdiData.UpdateAdi.Asset.Metadata.AMS.Version_Minor = newVersionMinor;
                //iterate any asset sections and update the version minor
                foreach (var item in packageEntry.AdiData.UpdateAdi.Asset.Asset.ToList()
                    .Where(item => item.Metadata.AMS.Version_Minor != newVersionMinor))
                    item.Metadata.AMS.Version_Minor = newVersionMinor;
                
                _logger.LogInformation($"Successfully updated All Version minor references to {newVersionMinor}");
                return Result.Success();
            }
            catch (Exception uavmvEx)
            {
                return _logger.LogErrorWithInnerException(uavmvEx, "UpdateAllVersionMinorValues",
                    "Error during update of version Minor");
            }
        }

        private Result CreatePackageAndDeliver(PackageEntry packageEntry, string packageName)
        {
            try
            {
                _logger.LogInformation($"Saving Update adi file to: {_tempWorkingDirectory}");
                _fileSystemWorker.SaveAdiFile(new DirectoryInfo(_tempWorkingDirectory), packageEntry.AdiData.UpdateAdi);
                _logger.LogInformation("Successfully Created Update ADI File.");

                var zipFile = packageEntry.IsTvodPackage ? $"TVOD_{packageName}.zip" : $"{packageName}.zip";
                var tmpPackage = CombinePath(_options.TempWorkingDirectory, zipFile);
                var zipResult = _zipArchiveWorker.CreateArchive(_tempWorkingDirectory, tmpPackage);
                if (zipResult.IsFailure)
                    throw new Exception($"Failed to create Delivery package: {tmpPackage}");
                
                _fileSystemWorker.DeliverZipArchive(zipFile);
                return Result.Success();
            }
            catch (IOException cpadException)
            {
                throw new Exception("Failure during Packaging and delivery", cpadException);
            }
        }
        
        public Result Cleanup()
        {
            if (!_workDirectoryCreated) return Result.Success();
            try
            {
                _logger.LogInformation($"Removing Temp working directory {_tempWorkingDirectory}");
                _fileSystemWorker.RemoveFolder(_tempWorkingDirectory);
                _logger.LogInformation("Successfully removed Temp working directory");
                return Result.Success();
            }
            catch (Exception cuException)
            {
                throw new Exception("Failure during Cleanup of package.", cuException);
            }
        }

        private string CombinePath(string path1, string path2)
        {
            var combinedPath = Path.Combine(path1, path2);
            return combinedPath.Replace('\\', '/');
        }
    }
}