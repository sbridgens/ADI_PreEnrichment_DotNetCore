using System;
using System.Linq;
using System.Text.RegularExpressions;
using Application.BusinessLogic.Contracts;
using Application.DataAccess.Persistence.Contracts;
using Application.FileManager.Serialization;
using Application.Models;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BusinessManager
{
    public class MetadataUpdater : IMetadataUpdater
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<MetadataUpdater> _logger;

        public MetadataUpdater(IApplicationDbContext context, ILogger<MetadataUpdater> logger)
        {
            _context = context;
            _logger = logger;
        }        
        
        public Result CopyPreviouslyEnrichedAssetDataToAdi(AdiData adiData, Guid ingestGuid, bool hasPreviewAsset,
            bool hasPreviousUpdate)
        {
            try
            {
                var dbImagesNullified = false;
                var enrichedDataHasImages = adiData.HasPreviousImageData(hasPreviousUpdate);
                var enrichedDataHasPreview = adiData.HasPreviewData(hasPreviousUpdate);

                ValidatePreviewData(adiData, hasPreviewAsset, enrichedDataHasPreview);


                foreach (var assetData in adiData.ExistingAssetData(hasPreviousUpdate))
                {
                    ProcessBlockPlatformFlag(adiData.Adi, assetData);
                    ProcessPreviewAsset(adiData, hasPreviewAsset, assetData);
                    ProcessPreviousImageData(adiData, ingestGuid, assetData, dbImagesNullified);
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                var message = $"[CopyPreviouslyEnrichedAssetDataToAdi] Error during Copy of previously enriched" +
                              $" asset data: {ex.Message}";
                _logger.LogError(ex, message);
                return Result.Failure(message);
            }
        }


        private void ValidatePreviewData(AdiData adiData, bool hasPreviewAsset, bool enrichedDataHasPreview)
        {
            //no enriched preview data,
            //no preview asset supplied preview metadata included via incoming adi
            if (!enrichedDataHasPreview && !hasPreviewAsset &&
                adiData.HasPreviewMetadata())
            {
                CheckPreviewData(adiData);
            }
        }
              
        private void ProcessBlockPlatformFlag(ADI adi, ADIAssetAsset assetData)
        {
            if (assetData.Metadata.AMS.Asset_Class != "movie") 
                return;

            var movieData = adi.Asset.Asset.FirstOrDefault(
                m => m.Metadata.AMS.Asset_Class == "movie");

            if (movieData != null)
            {
                /*
                    * Added function 20-07-2020 as the cp now controls add and remove of block platform
                    * this function will check the incoming update against the stored db version for existence of the block platform flag
                    * and persist or remove the flag based on the incoming file as the clone movie section maybe incorrect.
                    */
                //Asset Data Checks
                var hasBlockPlatformPreviously =
                    assetData.Metadata.App_Data.FirstOrDefault(b => b.Name == "Block_Platform");
                //MovieDataChecks
                var updateHasBlockPlatform =
                    movieData.Metadata.App_Data.FirstOrDefault(b => b.Name == "Block_Platform");

                if (hasBlockPlatformPreviously != null)
                    assetData.Metadata.App_Data.Remove(hasBlockPlatformPreviously);
                if (updateHasBlockPlatform != null)
                    assetData.Metadata.App_Data.Add(updateHasBlockPlatform);

                //add without content node
                var assetSection = new ADIAssetAsset
                {
                    Metadata = new ADIAssetAssetMetadata
                    {
                        //ensure incoming ams is used to maintain pricing information!
                        AMS = adi.Asset.Asset
                            .FirstOrDefault(m => m.Metadata.AMS.Asset_Class == "movie")
                            ?.Metadata.AMS,
                        App_Data = assetData.Metadata.App_Data
                    }
                };
                movieData.Content = null;
                movieData.Metadata = assetSection.Metadata;
            }
            else
            {
                AddNewMovieData(adi, assetData);
            }

        }
        
        private void ProcessPreviewAsset(AdiData adiData, bool hasPreviewAsset, ADIAssetAsset assetData)
        {
            if (assetData.Metadata.AMS.Asset_Class != "preview")
            {
                return;
            }

            if (!hasPreviewAsset)
            {
                //enriched = preview, adi = no preview asset and no preview metadata
                if (!adiData.HasPreviewMetadata())
                {
                    return;
                }

                var previewData = adiData.Adi.Asset.Asset
                    .FirstOrDefault(c => c.Metadata.AMS.Asset_Class == "preview");

                var previewAsset = new ADIAssetAsset
                {
                    Metadata = new ADIAssetAssetMetadata
                    {
                        AMS = assetData.Metadata.AMS,
                        App_Data = assetData.Metadata.App_Data
                    }
                };

                if (previewData == null) 
                    return;

                previewData.Content = null;
                previewData.Metadata = previewAsset.Metadata;
            }
            else
            {
                if (adiData.HasPreviewMetadata())
                {
                    _logger.LogInformation("Using supplied preview metadata as the update contains a physical asset plus preview metadata.");
                }
            }
        }

        private void ProcessPreviousImageData(AdiData adiData, Guid ingestGuid,ADIAssetAsset assetData, bool dbImagesNullified)
        {
            if (assetData.Metadata.AMS.Asset_Class != "image") 
                return;

            //compare db gn_images column
            var adiImage = assetData.Metadata.App_Data.FirstOrDefault(v => v.Name == "Image_Qualifier")?.Value;
            var match = Regex.Match(assetData.Content.Value, "(?m)p[0-9]{1,12}.*\\.[A-z]{3}");
            var imageMatch = string.Empty;

            if (match.Success)
            {
                if (dbImagesNullified == false)
                {
                    dbImagesNullified = CheckAdiImageMatchesDbImage(ingestGuid,
                        assetData.Metadata.App_Data.FirstOrDefault(i => i.Name == "Image_Qualifier")?.Value,
                        assetData.Content.Value);
                    
                }
            }


            var imageSection = new ADIAssetAsset
            {
                Content = new ADIAssetAssetContent
                {
                    Value = assetData.Content.Value
                },
                Metadata = new ADIAssetAssetMetadata
                {
                    AMS = assetData.Metadata.AMS,
                    App_Data = assetData.Metadata.App_Data
                }
            };
            adiData.Adi.Asset.Asset.Add(imageSection);
        }
        
        private bool CheckAdiImageMatchesDbImage(Guid ingestGuid, string imageType, string adiImage)
        {
            try
            {
                if (imageType != null & adiImage != null)
                {
                    var dbImages = _context.GN_Mapping_Data.First(i => i.IngestUUID == ingestGuid);

                    if (dbImages.GN_Images != null)
                    {
                        var imageName = Regex.Match(adiImage, "(?m)p[0-9]{1,12}.*\\.[A-z]{3}");
                        string imgMatch;

                        //if not found return
                        if (!imageName.Success)
                            return false;

                        imgMatch = $"{imageType}: assets/{imageName.Value}";
                        //check value in the db images
                        var imgCompare = Regex.Match(dbImages.GN_Images, imgMatch);
                        //value matches db images so return
                        if (imgCompare.Success)
                        {
                            return false;
                        }

                        //if we are here the adi does not match the db so nullify the db images to force a full update
                        _logger.LogWarning("DB Images mismatch against Stored adi images, resetting db images and forcing image update.");
                        dbImages.GN_Images = string.Empty;
                        _context.GN_Mapping_Data.Update(dbImages);
                        _context.SaveChanges();
                    }
                    else
                    {
                        _logger.LogWarning("Cannot check Adi Image Matches DB as the DB Images returned is Null.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CheckAdiImageMatchesDbImage] Error during Check of adi and DB Images");
                return false;
            }

            return true;
        }

        private void AddNewMovieData(ADI adi, ADIAssetAsset assetData)
        {
            var assetSection = new ADIAssetAsset
            {
                Metadata = new ADIAssetAssetMetadata
                {
                    //ensure incoming ams is used to maintain pricing information!
                    AMS = assetData.Metadata.AMS,
                    App_Data = assetData.Metadata.App_Data
                }
            };

            adi.Asset.Asset.Add(assetSection);
        }
        
        private void CheckPreviewData(AdiData adiData)
        {
            var hasUpdatePreviewData = false;
            if (adiData.UpdateAdi != null)
            {
                hasUpdatePreviewData = adiData.UpdateAdi.Asset.Asset.Any(p =>
                    p.Metadata.AMS.Asset_Class == "preview");
                
            }

            if (hasUpdatePreviewData)
            {
                return;
            }

            _logger.LogInformation($"Incoming asset has preview metadata, " +
                                    $"No Preview asset supplied and " +
                                    $"Enriched data does not contain preview metadata! " +
                                    $"removing preview data from adi.xml");

            var previewData = adiData.Adi.Asset.Asset
                .FirstOrDefault(c => c.Metadata.AMS.Asset_Class == "preview");

            adiData.Adi.Asset.Asset.Remove(previewData);
        }
    }
}