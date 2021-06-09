using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Application.FileManager.Serialization;
using CSharpFunctionalExtensions;
using Domain.Entities;

namespace Application.Models
{
    public class AdiData
    {
        private static readonly List<string> AdiNodesToRemove = new List<string>
        {
            //look at the set or update method to add if not exists
            //or update if it does, need a bool for crew/cast data.
            "Actors",
            "Actors_Display",
            "Block_Platform",
            "Director",
            "Episode_ID",
            "Episode_Name",
            "Episode_Ordinal",
            "Executive Producer",
            "ExtraData_1",
            "ExtraData_3",
            "GN_Layer1_TMSId",
            "GN_Layer2_RootId",
            "GN_Layer2_SeriesId",
            "GN_Layer2_TMSId",
            "Genre",
            "GenreID",
            "Producer",
            "Screenwriter",
            "Series_ID",
            "Series_Name",
            "Series_NumberOfItems",
            "Series_Ordinal",
            "Show_ID",
            "Show_Name",
            "Show_NumberOfItems",
            "Show_Summary_Short",
            "Summary_Short",
            "Title",
            "Writer"
        };

        public Adi_Data Entity { get; set; } = new Adi_Data();
        public ADI EnrichedAdi { get; set; }
        public ADI UpdateAdi { get; set; }
        public ADI Adi { get; set; }
        public string EnrichedAdiAsString { get; set; }

        public Result TrySetAdiAssetContentField(string assetClass, string assetFileName)
        {
            try
            {
                var contentFile = Adi
                    .Asset
                    .Asset
                    .FirstOrDefault(asset => $"{assetClass}".Equals(asset.Metadata.AMS.Asset_Class));

                if (contentFile == null)
                {
                    return Result.Failure("Asset metadata not found.");
                }

                contentFile.Content.Value = assetFileName;
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(ex.Message);
            }
        }

        public DateTime GetLicenceEndDate()
        {
            var txtDate = Adi.Asset.Metadata.App_Data
                .FirstOrDefault(l => l.Name.Equals("Licensing_Window_End"))?.Value;
            if (!DateTime.TryParse(txtDate, out var result))
            {
                throw new ArgumentException("Licensing_Window_End Is not a valid DateTime Format," +
                                            " Rejecting Ingest");
            }

            return result;
        }

        public int GetVersionMajor()
        {
            return Adi.Metadata.AMS.Version_Major;
        }

        public int GetVersionMinor()
        {
            return Adi.Metadata.AMS.Version_Minor;
        }

        public string GetProviderId()
        {
            return Adi.Metadata.AMS.Provider_ID;
        }

        public string GetAssetPaid(string assetType)
        {
            return Adi.Asset.Asset
                .Where(asset => asset.Metadata.AMS.Asset_Class.Equals(assetType))
                .Select(asset => asset.Metadata.AMS.Asset_ID.ToString()).FirstOrDefault();
        }

        public Result AddAssetMetadataApp_DataNode(string assetId, string nodeName, string nodeValue)
        {
            try
            {
                var nodeExists = Adi.Asset.Asset
                    .FirstOrDefault(a => a.Metadata.AMS.Asset_ID == assetId)
                    ?.Metadata.App_Data.FirstOrDefault(n => n.Name == nodeName);

                if (nodeExists != null)
                {
                    nodeExists.Value = nodeValue;
                }
                else
                {
                    var newAppData = new ADIAssetAssetMetadataApp_Data
                    {
                        App = "VOD",
                        Name = nodeName,
                        Value = nodeValue
                    };

                    Adi.Asset.Asset
                        .FirstOrDefault(a => a.Metadata.AMS.Asset_ID == assetId)
                        ?.Metadata.App_Data.Add(newAppData);
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(ex.Message);
            }
        }

        public Result InsertEpisodeData(string tmsid, string episodeOrdinalValue, string episodeTitle)
        {
            try
            {
                var result = Result.Combine(AddTitleMetadataApp_DataNode("Episode_ID", tmsid),
                    AddTitleMetadataApp_DataNode("Episode_Name", episodeTitle),
                    AddTitleMetadataApp_DataNode("Episode_Ordinal", episodeOrdinalValue));
                return result;
            }
            catch (Exception ex)
            {
                return Result.Failure($"[InsertEpisodeData] Error Setting Episode: {ex.Message}");
            }
        }

        public Result RemoveDefaultAdiNodes()
        {
            try
            {
                foreach (var adiNode in AdiNodesToRemove.SelectMany(item =>
                    Adi.Asset.Metadata.App_Data.Where(attr => attr.Name == item).ToList()))
                {
                    Adi.Asset.Metadata.App_Data.Remove(adiNode);
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(ex.Message);
            }
        }

        public Result AddTitleMetadataApp_DataNode(string nodeName, string nodeValue)
        {
            try
            {
                if (string.IsNullOrEmpty(nodeValue))
                {
                    return Result.Failure($"{nameof(nodeValue)} is NULL or empty");
                }

                var newAppData = new ADIAssetMetadataApp_Data
                {
                    App = "VOD",
                    Name = nodeName,
                    Value = nodeValue
                };

                Adi.Asset.Metadata.App_Data.Add(newAppData);

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Error Setting Metadata for Node {nodeName}: {ex.Message}");
            }
        }

        public bool HasPreviewMetadata()
        {
            return Adi.Asset.Asset.Any(e => e.Metadata.AMS.Asset_Class != null &&
                                            e.Metadata.AMS.Asset_Class.ToLower()
                                                .Equals("preview"));
        }

        public bool HasPreviousImageData(bool hasPreviousUpdate)
        {
            return hasPreviousUpdate
                ? UpdateAdi.Asset.Asset.Any(p => p.Metadata.AMS.Asset_Class == "image")
                : EnrichedAdi.Asset.Asset.Any(p => p.Metadata.AMS.Asset_Class == "image");
        }

        public bool HasPreviewData(bool hasPreviousUpdate)
        {
            return hasPreviousUpdate
                ? UpdateAdi.Asset.Asset.Any(p => p.Metadata.AMS.Asset_Class == "preview")
                : EnrichedAdi.Asset.Asset.Any(p => p.Metadata.AMS.Asset_Class == "preview");
        }

        public List<ADIAssetAsset> ExistingAssetData(bool hasPreviousUpdate)
        {
            return hasPreviousUpdate
                ? UpdateAdi.Asset.Asset.ToList()
                : EnrichedAdi.Asset.Asset.ToList();
        }
    }
}