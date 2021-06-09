using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Application.BusinessLogic.Contracts;
using Application.Configuration;
using Application.DataAccess.Persistence.Contracts;
using Application.FileManager.Contracts;
using Application.FileManager.Serialization;
using Application.Models;
using CSharpFunctionalExtensions;
using Domain.Entities;
using Infrastructure.DataAccess.GraceNoteApi;
using Infrastructure.FileManager;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.ImageLogic
{
    public class ImageWorker : IImageWorker
    {
        private readonly IApplicationDbContext _context;
        private readonly IFileSystemWorker _fileSystemWorker;
        private readonly IXmlSerializationManager _serializer;
        private readonly IGnMappingDataStore _gnMappingDataStore;
        private readonly ILogger<ImageWorker> _logger;
        private readonly EnrichmentSettings _options;

        public ImageWorker(IApplicationDbContext context,
            IGnMappingDataStore gnMappingDataStore,
            IFileSystemWorker fileSystemWorker,
            IXmlSerializationManager serializer,
            IOptions<EnrichmentSettings> options,
            ILogger<ImageWorker> logger)
        {
            _options = options.Value;
            _context = context;
            _gnMappingDataStore = gnMappingDataStore;
            _fileSystemWorker = fileSystemWorker;
            _serializer = serializer;
            _logger = logger;
        }

        public Result ProcessImages(PackageEntry entry)
        {
            return SelectAndSetImage(entry);
        }

        private Result SelectAndSetImage(PackageEntry entry)
        {
            //initialise as true and allow the workflow to falsify
            //this is needed for update packages as there is not always an updated image.

            var currentSelectedImage = "";


            foreach (var configLookup in _context.GN_ImageLookup.OrderBy(o => Convert.ToInt32(o.Image_AdiOrder)).ToList())
            {
                var mappingData = _serializer.Read<ImageMapping>(configLookup.Mapping_Config);
                var currentProgramType =
                    mappingData.ProgramType.SingleOrDefault(p =>
                        p == entry.GraceNoteData.MovieEpisodeProgramData.progType);

                // Ensure we don't use series or show assets for movies
                if (entry.IsMoviePackage & configLookup.Image_Mapping.ToLower().Contains("_series_") ||
                    entry.IsMoviePackage & configLookup.Image_Mapping.ToLower().Contains("_show_"))
                {
                    continue;
                }

                //prevent duplicate processing
                if (string.IsNullOrEmpty(currentProgramType) || configLookup.Image_Mapping == currentSelectedImage)
                {
                    continue;
                }


                var isl = GetIslData(mappingData, entry);

                var imageUri = isl.GetGracenoteImage(configLookup.Image_Lookup);

                if (string.IsNullOrEmpty(imageUri))
                {
                    continue;
                }

                if (!isl.DownloadImageRequired)
                {
                    continue;
                }

                //start download and adi update operations
                var localImage = GetImageName(imageUri, configLookup.Image_Mapping,
                    entry.ArchiveInfo.WorkingDirectory.FullName);
                var downloadResult = DownloadImage(imageUri, localImage);
                if (downloadResult.IsFailure)
                {
                    _logger.LogWarning($"Since downloading image \"{imageUri}\" failed, stopping image processing. Continue package processing");
                    return Result.Success();
                }

                var imagepaid = $"{isl.ImageQualifier}{entry.TitlPaIdValue.Replace("TITL", "")}";

                var insertResult = UpdateAdiWithImageData(entry, isl, configLookup, imagepaid, imageUri, localImage);

                //update image data in db and adi
                UpdateDbImages(entry, isl);

                if (insertResult.IsSuccess)
                {
                    currentSelectedImage = configLookup.Image_Mapping;
                }
            }

            return Result.Success();
        }

        private ImageSelectionLogic GetIslData(ImageMapping mappingData, PackageEntry entry)
        {
            var currentMappingData = _context.GN_Mapping_Data.FirstOrDefault(i => i.IngestUUID.Equals(entry.IngestUuid));
            var isl = new ImageSelectionLogic
            {
                ImageMapping = mappingData,
                CurrentMappingData = currentMappingData,
                IsUpdate = entry.IsPackageAnUpdate,
                ConfigImageCategories = mappingData.ImageCategory,
                ApiAssetList = entry.GraceNoteData.EnrichmentDataLists.ProgramAssets
            };

            isl.DbImagesForAsset = _gnMappingDataStore.ReturnDbImagesForAsset(
                entry.GraceNoteData.GnMappingPaid,
                isl.CurrentMappingData.Id,
                false
            );

            return isl;
        }

        private string GetImageName(string imageUri, string imageMapping, string currentWorkingDirectory)
        {
            var baseImage = imageUri.Replace("?trim=true", "");
            var originalFileName = Path.GetFileNameWithoutExtension(baseImage);

            var newFileName = originalFileName.Replace(originalFileName,
                $"{imageMapping}_{originalFileName}{Path.GetExtension(baseImage)}");
            return Path.Combine(currentWorkingDirectory, newFileName);
        }

        private Result UpdateAdiWithImageData(PackageEntry entry, ImageSelectionLogic isl, GN_ImageLookup configLookup,
            string imagepaid, string imageUri, string localImage)
        {
            var imageExists = entry.AdiData.Adi.Asset.Asset.FirstOrDefault(i => i.Metadata.AMS.Asset_ID == imagepaid);
            if (imageExists != null)
            {
                return UpdateImageData(
                    entry,
                    isl.ImageQualifier,
                    entry.TitlPaIdValue,
                    imageUri.Replace("assets/", ""),
                    configLookup.Image_Mapping,
                    isl.GetFileAspectRatio(localImage),
                    _fileSystemWorker.GetFileHash(localImage),
                    _fileSystemWorker.GetFileSize(localImage)
                );
            }
            //If its an update but the image is new then we will get a false back
            //therefore update the adi

            //download and insert image
            return InsertImageData
            (
                entry,
                entry.TitlPaIdValue,
                imageUri.Replace("assets/", ""),
                configLookup.Image_Mapping,
                _fileSystemWorker.GetFileHash(localImage),
                _fileSystemWorker.GetFileSize(localImage),
                Path.GetExtension(localImage),
                isl.ImageQualifier,
                configLookup.Image_Lookup,
                isl.GetFileAspectRatio(localImage)
            );
        }

        private Result UpdateImageData(
            PackageEntry entry,
            string imageQualifier,
            string titlPaid,
            string imageName,
            string imageMapping,
            string imageAspectRatio,
            string checksum,
            string filesize)
        {
            try
            {
                var paid = $"{imageQualifier}{titlPaid.Replace("TITL", "")}";
                var adiObject = entry.AdiData.Adi.Asset.Asset.FirstOrDefault(i => i.Metadata.AMS.Asset_ID == paid);

                if (adiObject == null)
                {
                    return Result.Failure($"Asset_ID not found: {paid}");
                }

                adiObject.Content.Value = ImageSelectionLogic.GetImageName(imageName, imageMapping);
                var cSum = adiObject.Metadata.App_Data.FirstOrDefault(c => c.Name == "Content_CheckSum");
                var fSize = adiObject.Metadata.App_Data.FirstOrDefault(s => s.Name == "Content_FileSize");
                var aRatio = adiObject.Metadata.App_Data.FirstOrDefault(a => a.Name == "Image_Aspect_Ratio");

                if (cSum != null)
                {
                    cSum.Value = checksum;
                }

                if (fSize != null)
                {
                    fSize.Value = filesize;
                }

                if (aRatio != null)
                {
                    aRatio.Value = imageAspectRatio;
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                var message = $"Error Encountered Updating Image Data: {ex.Message}";
                _logger.LogError(ex, message);
                return Result.Failure(message);
            }
        }

        private Result InsertImageData(
            PackageEntry entry,
            string titlPaid,
            string imageName,
            string imageMapping,
            string contentCheckSum,
            string contentFileSize,
            string encodingType,
            string imageQualifier,
            string imageLookupName,
            string imageAspectRatio
        )
        {
            try
            {
                var paid = $"{imageQualifier}{titlPaid.Replace("TITL", "")}";
                var adiObject = entry.AdiData.Adi.Asset.Asset.FirstOrDefault();

                if (adiObject != null)
                {
                    entry.AdiData.Adi.Asset.Asset.Add(new ADIAssetAsset
                    {
                        Content = new ADIAssetAssetContent
                        {
                            Value = ImageSelectionLogic.GetImageName(imageName, imageMapping)
                        },
                        Metadata = new ADIAssetAssetMetadata
                        {
                            AMS = new ADIAssetAssetMetadataAMS
                            {
                                Asset_Class = "image",
                                Asset_ID = paid,
                                Asset_Name = adiObject.Metadata.AMS.Asset_Name,
                                Creation_Date = adiObject.Metadata.AMS.Creation_Date,
                                Description = adiObject.Metadata.AMS.Description,
                                Product = "",
                                Provider = adiObject.Metadata.AMS.Provider,
                                Provider_ID = adiObject.Metadata.AMS.Provider_ID,
                                Verb = adiObject.Metadata.AMS.Verb,
                                Version_Major = adiObject.Metadata.AMS.Version_Major,
                                Version_Minor = adiObject.Metadata.AMS.Version_Minor
                            },
                            App_Data = new List<ADIAssetAssetMetadataApp_Data>()
                        }
                    });
                }

                entry.AdiData.AddAssetMetadataApp_DataNode(paid, "Content_CheckSum", contentCheckSum);
                entry.AdiData.AddAssetMetadataApp_DataNode(paid, "Content_FileSize", contentFileSize);
                entry.AdiData.AddAssetMetadataApp_DataNode(paid, "Encoding_Type", encodingType);
                entry.AdiData.AddAssetMetadataApp_DataNode(paid, "Image_Qualifier", imageLookupName);
                entry.AdiData.AddAssetMetadataApp_DataNode(paid, "Image_Aspect_Ratio", imageAspectRatio);
                entry.AdiData.AddAssetMetadataApp_DataNode(paid, "Type", "image");

                return Result.Success();
            }
            catch (Exception ex)
            {
                var message = $"Error Encountered Setting Image Data: {ex.Message}";
                _logger.LogError(ex, message);
                return Result.Failure(message);
            }
        }

        private void UpdateDbImages(PackageEntry entry, ImageSelectionLogic imageSelectionLogic)
        {
            var gnMappingRow = _context.GN_Mapping_Data.FirstOrDefault(i => i.IngestUUID.Equals(entry.IngestUuid));;
            gnMappingRow.GN_Images = imageSelectionLogic.DbImages;
            var created = _context.GN_Mapping_Data.Update(gnMappingRow);
            _context.SaveChanges();
            _logger.LogInformation($"GN Mapping table with row id: {created.Entity.Id} updated with new image data");
        }


        /// <summary>
        ///     Downloads the image from the configured image url and the image path found in the api
        /// </summary>
        private Result DownloadImage(string sourceImage, string destinationImage)
        {
            try
            {
                using (var webClient = new WebClientManager())
                {
                    var downloadUrl = $"{_options.MediaCloud}/{sourceImage}";

                    webClient.DownloadWebBasedFile(
                        downloadUrl,
                        false,
                        destinationImage);

                    _logger.LogInformation($"Successfully Downloaded Image: {sourceImage} as {destinationImage}");
                    return Result.Success();
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"[DownloadImage] Error Downloading Image: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                return Result.Failure(errorMessage);
            }
        }
    }
}