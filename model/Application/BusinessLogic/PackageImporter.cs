using System;
using System.IO;
using System.Linq;
using Application.BusinessLogic.Contracts;
using Application.Configuration;
using Application.FileManager.Contracts;
using Application.FileManager.Serialization;
using Application.Models;
using Application.Validation.Contracts;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.BusinessLogic
{
    public class PackageImporter : IPackageImporter
    {
        private readonly IAdiValidator _adiValidator;
        private readonly IAdiImporter _importer;
        private readonly ILogger<PackageImporter> _logger;
        private readonly EnrichmentSettings _options;
        private readonly IZipArchiveWorker _zipWorker;

        public PackageImporter(IZipArchiveWorker zipWorker,
            IAdiImporter importer,
            IAdiValidator adiValidator,
            IOptions<EnrichmentSettings> options,
            ILogger<PackageImporter> logger)
        {
            _zipWorker = zipWorker;
            _importer = importer;
            _adiValidator = adiValidator;
            _logger = logger;
            _options = options.Value;
        }

        public Result<PackageEntry> TryImportPackage(FileInfo package)
        {
            var maybeAdi = _zipWorker.ExtractAdiXml(package, _options.TempWorkingDirectory);
            if (maybeAdi.HasNoValue)
            {
                return Result.Failure<PackageEntry>($"Could not find ADI.xml file in package {package.FullName}.");
            }

            var adiXmlFile = maybeAdi.Value.FullName;
            var adiResult = _importer.ReadFromXml(adiXmlFile);
            if (adiResult.IsFailure)
            {
                CleanupFolder(maybeAdi.Value.Directory);
                return Result.Failure<PackageEntry>(
                    $"Failed to import ADI definition from XML file: {adiResult.Error}.");
            }

            var archiveInfo = _zipWorker.ReadArchiveInfo(package);
            archiveInfo.WorkingDirectory = maybeAdi.Value.Directory;
            archiveInfo.ExtractedAdiFile = maybeAdi.Value;
            var entry = new PackageEntry
            {
                AdiData = new AdiData
                {
                    Adi = adiResult.Value
                },
                ArchiveInfo = archiveInfo,
            };

            return ValidateAndSetPaIdValue(entry)
                .Bind(DetectAndSetPackageType);
        }

        private Result<PackageEntry> DetectAndSetPackageType(PackageEntry entry)
        {
            try
            {
                entry.IsPackageAnUpdate = !entry.ArchiveInfo.HasMediaFolder;
                entry.IsSdPackage = IsSdPackage(entry);
                entry.IsAdult = IsAdult(entry.AdiData.Adi);
                entry.IsUltraHd = IsUltraHd(entry.AdiData.Adi);
                entry.IsTvodPackage = IsTvodAsset(entry.AdiData.Adi);
                entry.HasPreviewAssets = HasPreviewMetadata(entry.AdiData.Adi);
                entry.HasPoster = CheckAndRemovePoster(entry.AdiData.Adi);
                return Result.Success(entry);
            }
            catch (Exception ex)
            {
                var message = $"Failed during {nameof(DetectAndSetPackageType)}";
                _logger.LogError(ex, message);
                return Result.Failure<PackageEntry>(message);
            }
        }

        private bool CheckAndRemovePoster(ADI adi)
        {
            var poster =
                adi.Asset.Asset.FirstOrDefault(p =>
                    p.Metadata.AMS.Asset_Class == "poster");

            if (poster == null)
            {
                return false;
            }

            _logger.LogInformation("Asset contains a Poster, removing from Package");
            adi.Asset.Asset.Remove(poster);
            return true;
        }

        private bool HasPreviewMetadata(ADI adi)
        {
            var result = adi.Asset.Asset.Any(e => e.Metadata.AMS.Asset_Class != null &&
                                                  e.Metadata.AMS.Asset_Class.ToLower()
                                                      .Equals("preview"));
            if (result)
            {
                _logger.LogInformation("Package Contains a Preview Metadata.");
            }

            return result;
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

        private bool IsUltraHd(ADI adi)
        {
            try
            {
                var uhd = adi.Asset.Asset?
                    .FirstOrDefault()?
                    .Metadata
                    .App_Data
                    .FirstOrDefault(u => u.Name.Equals("Encoding_Type"))
                    ?.Value;

                return uhd?.ToLower() == "h264-uhd";
            }
            catch (Exception iupException)
            {
                _logger.LogError("Error During Detection of UHD Package type", iupException);
                return false;
            }
        }

        private bool IsAdult(ADI adi)
        {
            {
                try
                {
                    var audience =
                        adi.Asset.Metadata.App_Data
                            .FirstOrDefault(a => a.Name == "Audience")?.Value;

                    return audience?.ToLower() == "adult";
                }
                catch (Exception iapEx)
                {
                    _logger.LogError("Error During Detection of Audience Type", iapEx);
                    return false;
                }
            }
        }

        private Result<PackageEntry> ValidateAndSetPaIdValue(PackageEntry entry)
        {
            entry.TitlPaIdValue = entry.AdiData.Adi.Asset.Metadata.AMS.Asset_ID;
            _logger.LogInformation("XML well formed, Retrieving PAID Value from ADI to use in Gracenote Mapping Lookup");
            var validation = Result.Try(() => _adiValidator.ValidatePaidValue(entry.AdiData.Adi), 
                exception =>
                {
                    var message = "Failed to parse PAID value";
                    _logger.LogError(message, exception);
                    return message;
                });

            if (validation.IsFailure)
            {
                return Result.Failure<PackageEntry>(validation.Error);
            }
            
            entry.OnApiProviderId = validation.Value.OnapiProviderid;
            if (validation.Value.IsQamAsset)
            {
                entry.AdiData.Adi.Asset.Metadata.AMS.Asset_ID = validation.Value.NewTitlPaid;
            }

            if (!string.IsNullOrEmpty(validation.Value.NewTitlPaid))
            {
                entry.TitlPaIdValue = validation.Value.NewTitlPaid;
            }

            entry.IsQamAsset = validation.Value.IsQamAsset;
            return Result.Success(entry);
        }

        private bool IsSdPackage(PackageEntry entry)
        {
            if (entry.IsPackageAnUpdate)
            {
                return true;
            }

            var result = false;
            var adiAssetAssetMetadata = entry.AdiData.Adi.Asset.Asset?.FirstOrDefault()?.Metadata;

            if (adiAssetAssetMetadata != null)
            {
                result = adiAssetAssetMetadata.App_Data
                    .FirstOrDefault(c => c.Name.ToLower() == "hdcontent")
                    ?.Value.ToLower() != "y";
            }

            if (result & !Convert.ToBoolean(_options.AllowSDContentIngest))
            {
                throw new InvalidOperationException(
                    $"SD Content Detected, Configuration disallows SD Content from Ingest; Failing ingest for {entry.TitlPaIdValue}");
            }

            if (result)
            {
                _logger.LogInformation("Content is marked as SD Content, Configuration allows SD content for ingest.");
            }

            return result;
        }

        private void CleanupFolder(DirectoryInfo folder)
        {
            if (!folder.Exists)
            {
                return;
            }

            _logger.LogInformation($"Deleting folder: {folder.FullName}");
            folder.Delete(true);
        }
    }
}