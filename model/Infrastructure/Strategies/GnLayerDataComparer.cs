using System;
using System.Linq;
using System.Threading.Tasks;
using Application.Models;
using Application.BusinessLogic.Contracts;
using Application.FileManager.Serialization;
using Application.DataAccess.Persistence.Contracts;
using Application.Validation.Contracts;
using CSharpFunctionalExtensions;
using Domain.Entities;
using Infrastructure.ImageLogic;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Strategies
{
    public class GnLayerDataComparer : IGnLayerDataComprarer
    {
        private readonly ILogger<GnLayerDataComparer> _logger;
        private GN_Mapping_Data DbEnrichedMappingData { get; set; }
        private readonly IApplicationDbContext _dbContext;
        private readonly IProgramTypeLookupStore _programTypeLookupStore;
        private readonly IDataFetcher _dataFetcher;
        private readonly IXmlSerializationManager _serializer;
        private readonly IGnMappingDataStore _mappingDataStore;
        private readonly IPackageValidator _packageValidator;

        public GnLayerDataComparer(IApplicationDbContext dbContext, IDataFetcher dataFetcher, IXmlSerializationManager serializer, 
           IGnMappingDataStore mappingDataStore,  IProgramTypeLookupStore programTypeLookupStore,
           IPackageValidator packageValidator, ILogger<GnLayerDataComparer> logger)
        {
            _dbContext = dbContext;
            _dataFetcher = dataFetcher;
            _serializer = serializer;
            _programTypeLookupStore = programTypeLookupStore;
            _mappingDataStore = mappingDataStore;
            _packageValidator = packageValidator;
            _logger = logger;
        }

        public async Task<bool> ProgramDataChanged(PackageEntry packageEntry, Guid ingestUuid, int layer)
        {
            try
            {
                var apiProgramData = _dbContext.GN_Api_Lookup.FirstOrDefault(m => m.IngestUUID == ingestUuid);
                DbEnrichedMappingData = _dbContext.GN_Mapping_Data.FirstOrDefault(m => m.IngestUUID == ingestUuid);
                if (DbEnrichedMappingData == null)
                {
                    _logger.LogError($"Gracenote mapping data with ingest uuid {ingestUuid} is absent in the database." +
                                     $"This is not a valid scenario, aborting layer{layer} enrichment!");
                    return false;
                }

                var collectionResult = await EnsureLayerCollection(packageEntry, apiProgramData);
                if (collectionResult.IsFailure)
                {
                    _logger.LogError(collectionResult.Error);
                    return false;
                }

                apiProgramData = collectionResult.Value;

                if (packageEntry.GraceNoteData.MovieEpisodeProgramData == null) return false;
                CheckAndUpdateTmsId(apiProgramData);
                PopulateDataLayer(packageEntry, layer);
                
                if (packageEntry.GraceNoteData.MovieEpisodeProgramData == null) return false;
                var dbAdiData = _dbContext.Adi_Data.FirstOrDefault(i => i.IngestUUID.Equals(ingestUuid));
                var loadResult = LoadEnrichedAdiFile(packageEntry, ingestUuid);
                if (loadResult.IsFailure)
                {
                    _logger.LogError(loadResult.Error);
                    return false;
                }

                _programTypeLookupStore.SetProgramType(packageEntry, true);
                return _packageValidator.IsValidForEnrichment(packageEntry, dbAdiData) 
                       || CheckImageData(packageEntry);
            }
            catch (Exception pdcException)
            {
                _logger.LogError(pdcException, "[GnLayerDataComparer] Pdc exception occured in");
                return false;
            }
        }

        private async Task<Result<GN_Api_Lookup>> EnsureLayerCollection(PackageEntry packageEntry, GN_Api_Lookup apiProgramData)
        {
            //ensures that we collect layer data if there was a db or tracker service issue
            if (apiProgramData == null)
            {
                var collectionResult = await _dataFetcher.FetchLayer(DbEnrichedMappingData, 1, DbEnrichedMappingData.GN_TMSID,
                    packageEntry)
                    .Bind(async(result) =>
                    {
                        apiProgramData = result; 
                        return await _dataFetcher.GetGracenoteMovieEpisodeData(apiProgramData, packageEntry, DbEnrichedMappingData);
                    })
                    .Bind(async() => await _dataFetcher.GetGracenoteMovieEpisodeData(apiProgramData, packageEntry, DbEnrichedMappingData))
                    .Bind(async() => await _dataFetcher.FetchLayer(DbEnrichedMappingData, 2, DbEnrichedMappingData.GN_connectorId, packageEntry))
                    .Bind(async (result) => 
                    { 
                        apiProgramData = result;
                        return await _dataFetcher.GetSeriesSeasonSpecialsData(apiProgramData, packageEntry,
                            DbEnrichedMappingData);
                    });
                if (collectionResult.IsFailure) return Result.Failure<GN_Api_Lookup>(collectionResult.Error);
            }
            
            if (apiProgramData.GnLayer1Data == null)
            {
                var result = await _dataFetcher.FetchLayer(DbEnrichedMappingData, 1, DbEnrichedMappingData.GN_TMSID, packageEntry);
                if (result.IsFailure) return Result.Failure<GN_Api_Lookup>(result.Error);
            }            
            await _dataFetcher.GetGracenoteMovieEpisodeData(apiProgramData, packageEntry, DbEnrichedMappingData);
            
            if (apiProgramData.GnLayer2Data == null)
            {
                var result = await _dataFetcher.FetchLayer(DbEnrichedMappingData, 2, DbEnrichedMappingData.GN_connectorId, packageEntry);
                if (result.IsFailure) return Result.Failure<GN_Api_Lookup>(result.Error);
            }
            await _dataFetcher.GetSeriesSeasonSpecialsData(apiProgramData, packageEntry, DbEnrichedMappingData);
            return Result.Success(apiProgramData);
        }

        private void CheckAndUpdateTmsId(GN_Api_Lookup apiProgramData)
        {
            if (DbEnrichedMappingData.GN_TMSID == apiProgramData.GN_TMSID) return;
            apiProgramData.GN_TMSID = DbEnrichedMappingData.GN_TMSID;
            _dbContext.GN_Api_Lookup.Update(apiProgramData);
            _dbContext.SaveChanges();
        }

        private void PopulateDataLayer(PackageEntry packageEntry, int layer)
        {
            var programs = packageEntry.GraceNoteData.CoreProgramData.programs;
            packageEntry.GraceNoteData.MovieEpisodeProgramData = layer == 1
                ? programs.FirstOrDefault(t => t.TMSId == packageEntry.GraceNoteData.MovieEpisodeProgramData.TMSId)
                : programs.FirstOrDefault(c => c.connectorId == programs.FirstOrDefault()?.connectorId);
        }

        private Result LoadEnrichedAdiFile(PackageEntry packageEntry, Guid ingestUuid)
        {
            var dbAdi = _dbContext.Adi_Data.FirstOrDefault(i => i.IngestUUID.Equals(ingestUuid));
            if (dbAdi?.EnrichedAdi == null)
            {
                throw new Exception($"Previously Enriched ADI data for Paid: " +
                                    $"{packageEntry.TitlPaIdValue} was not found in the database?");
            }

            return SerializeUpdateData(dbAdi, packageEntry.AdiData);
        }
        
        private Result SerializeUpdateData(Adi_Data dbEntity, AdiData packageAdiData)
        {
            try
            {
                var adi = dbEntity.UpdateAdi ?? dbEntity.EnrichedAdi;
                packageAdiData.UpdateAdi = _serializer.Read<ADI>(adi);
                if (packageAdiData.Adi == null) return Result.Success();
                packageAdiData.Entity.VersionMajor = packageAdiData.GetVersionMajor();
                packageAdiData.Entity.VersionMinor = packageAdiData.GetVersionMinor();

                return Result.Success();
            }
            catch (Exception e)
            {
                _logger.LogError("Error while serializing update data.", e);
                return Result.Failure("Failed to serialize update data");
            }
        }

        private bool CheckImageData(PackageEntry packageEntry)
        {
            try
            {
                var imageLookups = _dbContext.GN_ImageLookup.OrderBy(o => Convert.ToInt32(o.Image_AdiOrder));
                foreach (var configLookup in imageLookups)
                {
                    var result = CheckConfigLookup(packageEntry, configLookup);
                    if (result) return true;
                }

                return false;
            }
            catch (Exception e)
            {
                _logger.LogError("Check image data error in gn layer data comparer", e);
                throw;
            }
        }

        private bool CheckConfigLookup(PackageEntry packageEntry, GN_ImageLookup configLookup)
        {
            var mappingData = _serializer.Read<ImageMapping>(configLookup.Mapping_Config);
            var currentProgramType = mappingData.ProgramType
                .SingleOrDefault(p => p == packageEntry.GraceNoteData.MovieEpisodeProgramData.progType);
            var imageMapping = configLookup.Image_Mapping.ToLower();
                    
            if(packageEntry.IsMoviePackage 
               && (imageMapping.Contains("_series_") || imageMapping.Contains("_show_")))
                return false;

            var currentImage = "";
            //prevent duplicate processing
            if (string.IsNullOrEmpty(currentProgramType) ||
                configLookup.Image_Mapping == currentImage)
                return false;

            return CheckImageSelectionLogic(packageEntry.GraceNoteData.EnrichmentDataLists, mappingData, configLookup);
        }

        private bool CheckImageSelectionLogic(EnrichmentDataLists enrichmentDataLists, ImageMapping mappingData, GN_ImageLookup configLookup)
        {
            var isl = new ImageSelectionLogic
            {
                ImageMapping = mappingData,
                CurrentMappingData = _dbContext.GN_Mapping_Data.FirstOrDefault(m => m.IngestUUID.Equals(DbEnrichedMappingData.IngestUUID)),
                IsUpdate = true,
                ConfigImageCategories = mappingData.ImageCategory,
                ApiAssetList = enrichmentDataLists.ProgramAssets?.ToList()
            };
            
            isl.DbImagesForAsset = _mappingDataStore.ReturnDbImagesForAsset(
                DbEnrichedMappingData.GN_Paid,
                isl.CurrentMappingData.Id,
                true
            );

            var imageUri = isl.GetGracenoteImage(configLookup.Image_Lookup, true);
            if (string.IsNullOrEmpty(imageUri) || !isl.DownloadImageRequired) return false;
            
            _logger.LogDebug($"Image Data change: {configLookup.Image_Lookup} - {imageUri}");
            return true;
        }
    }
}