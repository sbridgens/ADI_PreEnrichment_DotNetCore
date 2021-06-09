using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Application.FileManager.Contracts;
using Application.Models;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;

namespace Infrastructure.FileManager.ZipArchive
{
    public class ZipArchiveWorker : IZipArchiveWorker
    {
        private const string AdiFilename = "adi.xml";
        private const string ZipFileExtension = ".zip";
        private const string MediaFolder = "media/";
        private const string PreviewFolder = "preview/";
        private readonly ILogger<ZipArchiveWorker> _logger;

        public ZipArchiveWorker(ILogger<ZipArchiveWorker> logger)
        {
            _logger = logger;
        }

        public Maybe<FileInfo> ExtractAdiXml(FileInfo archive, string workingFolder)
        {
            if (!archive.Extension.Equals(ZipFileExtension))
            {
                throw new ArgumentException($"Unexpected file format: {archive.Extension}");
            }
            var tempFolder = Path.Combine(workingFolder, Path.GetFileNameWithoutExtension(archive.Name));
            using (var zip = ZipFile.OpenRead(archive.FullName))
            {
                foreach (var entry in zip.Entries)
                {
                    if (!entry.Name.ToLowerInvariant().Equals(AdiFilename))
                    {
                        continue;
                    }

                    CreateTempFolder(tempFolder);
                    var destinationFile = Path.Combine(tempFolder, AdiFilename);
                    entry.ExtractToFile(destinationFile);
                    return Maybe<FileInfo>.From(new FileInfo(destinationFile));
                }
            }

            return Maybe<FileInfo>.None;
        }

        public ArchiveInfo ReadArchiveInfo(FileInfo archive)
        {
            var result = new ArchiveInfo
            {
                Archive = archive
            };

            using var zip = ZipFile.OpenRead(archive.FullName);
            if (zip.Entries.Any(e => e.FullName.ToLowerInvariant().Contains(MediaFolder)))
            {
                result.HasMediaFolder = true;
            }

            if (zip.Entries.Any(p => p.FullName.ToLowerInvariant().Contains(PreviewFolder)))
            {
                result.HasPreviewAssets = true;
            }

            return result;
        }

        public Result ExtractArchive(ArchiveInfo archive, bool isUpdate)
        {
            try
            {
                ExtractLargestEntries(archive, isUpdate);
                return Result.Success();
            }
            catch (Exception e)
            {
                var message = $"Error extracting archive: {archive.Archive.Name}";
                _logger.LogError(e, message);
                return Result.Failure(message);
            }
        }

        public Result CreateArchive(string sourceDirectory, string destinationArchive)
        {
            try
            {
                if (File.Exists(destinationArchive))
                {
                    File.Delete(destinationArchive);
                }

                _logger.LogInformation(
                    $"Packaging Source directory: {sourceDirectory} to Zip Archive: {destinationArchive}");
                ZipFile.CreateFromDirectory(sourceDirectory, destinationArchive, CompressionLevel.Fastest, false);
                _logger.LogInformation($"Zip Archive: {destinationArchive} created Successfully.");
                return Result.Success();
            }
            catch (Exception ex)
            {
                var message = $"Failed to Create Zip package: {destinationArchive} - {ex.Message}";
                _logger.LogError(ex, message);
                return Result.Failure(message);
            }
        }

        private void CreateTempFolder(string folderPath)
        {
            var tempFolder = new DirectoryInfo(folderPath);
            if (tempFolder.Exists)
            {
                tempFolder.Delete(true);
            }

            tempFolder.Create();
        }

        private void ExtractLargestEntries(ArchiveInfo packageArchiveInfo, bool isUpdate)
        {
            using var zip = ZipFile.OpenRead(packageArchiveInfo.Archive.FullName);
            foreach (var entry in zip.Entries.OrderByDescending(e => e.Length))
            {
                if (!packageArchiveInfo.StlExtracted &&
                    Path.GetExtension(entry.FullName) == ".stl")
                {
                    _logger.LogInformation($"Extracting Subtitle file: {entry.Name}");
                    ExtractEntry(packageArchiveInfo, entry, "stl");
                }
                else
                {
                    if (!isUpdate)
                    {
                        if (!packageArchiveInfo.MovieAssetExtracted &&
                            entry.FullName.Contains("media/"))
                        {
                            _logger.LogInformation($"Extracting Largest .ts file: {entry.Name} from Package");
                            ExtractEntry(packageArchiveInfo, entry, "movie");
                        }

                        if (packageArchiveInfo.PreviewExtracted ||
                            !entry.FullName.Contains("preview/"))
                        {
                            continue;
                        }

                        _logger.LogInformation($"Extracting Largest Preview Asset {entry.Name} from Package.");
                        ExtractEntry(packageArchiveInfo, entry, "preview");
                    }
                    else if (!packageArchiveInfo.PreviewExtracted &&
                             packageArchiveInfo.HasPreviewAssets &&
                             entry.FullName.Contains("preview/"))
                    {
                        _logger.LogInformation($"Extracting Largest Preview Asset {entry.Name} from Package.");
                        ExtractEntry(packageArchiveInfo, entry, "preview");
                    }
                }
            }
        }

        private void ExtractEntry(ArchiveInfo packageArchiveInfo, ZipArchiveEntry archiveEntry, string entryType)
        {
            var entrySize = archiveEntry.Length;
            var outputFile = Path.Combine(packageArchiveInfo.WorkingDirectory.FullName, archiveEntry.Name);
            var entryFileInfo = new FileInfo(outputFile);
            CheckWorkingDirectory(packageArchiveInfo.WorkingDirectory.FullName);
            archiveEntry.ExtractToFile(outputFile, true);
            if (entryFileInfo.Length != entrySize)
            {
                return;
            }

            _logger.LogInformation($"Successfully extracted {archiveEntry.Name} to {outputFile}");
            switch (entryType)
            {
                case "adi":
                    packageArchiveInfo.ExtractedAdiFile = entryFileInfo;
                    packageArchiveInfo.AdiExtracted = true;
                    break;
                case "movie":
                    packageArchiveInfo.ExtractedMovieAsset = entryFileInfo;
                    packageArchiveInfo.MovieAssetExtracted = true;
                    break;
                case "preview":
                    packageArchiveInfo.ExtractedPreview = entryFileInfo;
                    packageArchiveInfo.PreviewExtracted = true;
                    break;
                case "stl":
                    packageArchiveInfo.ExtractedSubtitle = entryFileInfo;
                    packageArchiveInfo.StlExtracted = true;
                    break;
            }
        }

        private void CheckWorkingDirectory(string directory)
        {
            if (Directory.Exists(directory))
            {
                return;
            }

            _logger.LogInformation($"Creating Working Directory: {directory}");
            Directory.CreateDirectory(directory);
        }
    }
}