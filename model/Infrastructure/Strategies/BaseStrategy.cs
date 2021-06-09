using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Application.BusinessLogic.Contracts;
using Application.Configuration;
using Application.DataAccess.Persistence.Contracts;
using Application.Extensions;
using Application.FileManager.Contracts;
using Application.Models;
using CSharpFunctionalExtensions;
using Domain.Entities;
using Domain.Schema.GNProgramSchema.Extensions;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Strategies
{
    public abstract class BaseStrategy : IProcessingStrategy
    {
        private readonly IApplicationDbContext _context;
        private readonly IFileSystemWorker _fileSystemWorker;
        private readonly IGnMappingDataStore _gnMappingDataStore;
        private readonly IGraceNoteMetadataProvider _graceNote;
        private readonly IImageWorker _imageWorker;
        private readonly ILogger<BaseStrategy> _logger;
        private readonly IMetadataMapper _metadataMapper;
        private readonly EnrichmentSettings _options;
        private readonly ISystemClock _systemClock;
        private readonly IZipArchiveWorker _zipWorker;

        protected BaseStrategy(IApplicationDbContext context,
            IFileSystemWorker fileSystemWorker,
            IGraceNoteMetadataProvider graceNote,
            IZipArchiveWorker zipWorker,
            ISystemClock systemClock,
            IMetadataMapper metadataMapper,
            IGnMappingDataStore gnMappingDataStore,
            IImageWorker imageWorker,
            ILoggerFactory logger,
            IOptions<EnrichmentSettings> options)
        {
            _context = context;
            _fileSystemWorker = fileSystemWorker;
            _graceNote = graceNote;
            _zipWorker = zipWorker;
            _systemClock = systemClock;
            _metadataMapper = metadataMapper;
            _gnMappingDataStore = gnMappingDataStore;
            _imageWorker = imageWorker;
            _logger = logger.CreateLogger<BaseStrategy>();
            _options = options.Value;
        }

        public virtual async Task<Result> Execute(PackageEntry entry)
        {
            var result = await GetGracenoteMovieEpisodeData(entry)
                .Bind(SetAdiMovieEpisodeMetadata);
            
            if (!entry.IsMoviePackage)
            {
                await result.Bind(() => ProcessSeriesEpisodePackage(entry));
            }

            return result
                .Bind(() => _imageWorker.ProcessImages(entry))
                .Bind(() => RemovedDerivedFromAsset(entry))
                .Bind(() => FinalisePackageData(entry))
                .Bind(() => UpdateAdiVersions(entry))
                .Bind(() => SaveAdiFile(entry))
                .Bind(() => PackageEnrichedAsset(entry))
                .Bind(deliveryPackage => _fileSystemWorker.DeliverEnrichedAsset(deliveryPackage, entry.IsTvodPackage));
        }

        protected async Task<Result> ProcessSeriesEpisodePackage(PackageEntry entry)
        {
            return await GetSeriesSeasonSpecialsData(entry)
                .Bind(() => SetAdiSeriesData(entry))
                .Bind(() => SetAdiSeasonData(entry));
        }

        protected Result SetAdiSeasonData(PackageEntry entry)
        {
            return _metadataMapper.InsertProductionYears(
                entry.GraceNoteData.GetSeriesPremiere(),
                entry.GraceNoteData.GetSeasonPremiere(),
                entry.GraceNoteData.GetSeriesFinale(),
                entry.GraceNoteData.GetSeasonFinale(),
                entry.AdiData);
        }

        protected Result SetAdiSeriesData(PackageEntry entry)
        {
            entry.GraceNoteData.SeasonInfo = entry.GraceNoteData.ShowSeriesSeasonProgramData?.seasons ??
                                             entry.GraceNoteData.MovieEpisodeProgramData.seasons;

            return
                //Insert IMDB Data
                _metadataMapper.InsertIdmbData(
                        entry.GraceNoteData.ExternalLinks(),
                        entry.AdiData,
                        entry.GraceNoteData.HasMovieInfo())
                    .Bind(() => _metadataMapper.InsertSeriesLayerData(
                        entry.GraceNoteData.ShowSeriesSeasonProgramData.connectorId,
                        entry.GraceNoteData.MovieEpisodeProgramData.GetSeriesId(), entry.AdiData))
                    .Bind(() => _metadataMapper.InsertShowData(
                        entry.GraceNoteData.GetShowId(_options.Prefix_Show_ID_Value),
                        entry.GraceNoteData.GetShowName(),
                        entry.GraceNoteData.GetNumberOfSeasons(),
                        entry.GraceNoteData.ShowSeriesSeasonProgramData.descriptions,
                        entry.GraceNoteData.SeasonInfo.Any(),
                        entry.AdiData))
                    .Bind(() => _metadataMapper.InsertSeriesGenreData(entry.GraceNoteData.EnrichmentDataLists,
                        entry.AdiData))
                    .Bind(() => _metadataMapper.InsertSeriesData(
                        entry.GraceNoteData.MovieEpisodeProgramData.GetGnSeriesId(),
                        entry.GraceNoteData.MovieEpisodeProgramData.GetSeriesOrdinalValue(),
                        entry.GraceNoteData.SeasonInfo,
                        entry.GraceNoteData.MovieEpisodeProgramData.GetSeasonId(),
                        entry.GraceNoteData.MovieEpisodeProgramData.GetEpisodeSeason(),
                        entry.AdiData,
                        _options.Prefix_Series_ID_Value
                    ));
        }

        protected async Task<Result> GetSeriesSeasonSpecialsData(PackageEntry entry)
        {
            return
                await _graceNote.RetrieveAndAddSeriesSeasonSpecialsData(entry)
                    .Bind(SetInitialLayer2SeriesData)
                    .OnSuccessTry(() =>
                    {
                        entry.GraceNoteData.EnrichmentDataLists.UpdateListData(
                            entry.GraceNoteData.ShowSeriesSeasonProgramData,
                            entry.SeasonId.ToString());
                    });
        }

        protected Result SetInitialLayer2SeriesData(PackageEntry entry)
        {
            var seriesData = entry.GraceNoteData.ShowSeriesSeasonProgramData;
            entry.SeasonId = Convert.ToInt32(seriesData?.seasonId);

            if (seriesData?.seasons != null && seriesData.seasons.Any())
            {
                var seasonIdString = seriesData
                    .seasons?
                    .First(s => s.seasonId == entry.GraceNoteData.MovieEpisodeProgramData.seasonId)
                    .seasonId;
                entry.SeasonId = Convert.ToInt32(seasonIdString);
                _logger.LogInformation($"Program contains Season data for season ID: {entry.SeasonId}");
            }

            return Result.Success();
        }

        protected Result InsertEpisodeData(PackageEntry entry)
        {
            //add episode info for non movie packages
            if (entry.IsMoviePackage)
            {
                return Result.Success();
            }

            return entry.AdiData.InsertEpisodeData(
                entry.GraceNoteData.GraceNoteTmsId,
                entry.GraceNoteData.MovieEpisodeProgramData.GetEpisodeOrdinalValue(),
                entry.GraceNoteData.MovieEpisodeProgramData.GetEpisodeTitle());
        }

        protected Result SetAdiMovieEpisodeMetadata(PackageEntry entry)
        {
            try
            {
                entry.AdiData.RemoveDefaultAdiNodes();

                var insertEpisodeDataResult = InsertEpisodeData(entry);
                if (insertEpisodeDataResult.IsFailure)
                {
                    return insertEpisodeDataResult;
                }

                //Get and add GN Program Data
                //TODO: re-examine
                var result = _gnMappingDataStore.AddGraceNoteProgramData(
                        entry.IngestUuid,
                        entry.GraceNoteData.MovieEpisodeProgramData.GetSeriesTitle(),
                        entry.GraceNoteData.MovieEpisodeProgramData.GetEpisodeTitle(),
                        entry.GraceNoteData.MovieEpisodeProgramData
                    )
                    //Insert Crew Actor Data
                    .Bind(() => _metadataMapper.InsertActorData(entry.GraceNoteData.EnrichmentDataLists, entry.AdiData))
                    //Insert Support Crew Data
                    .Bind(() => _metadataMapper.InsertCrewData(entry.GraceNoteData.EnrichmentDataLists, entry.AdiData))
                    //Insert Program Title Data
                    .Bind(() => _metadataMapper.InsertTitleData(entry.GraceNoteData.EnrichmentDataLists, entry.AdiData,
                        entry.IsMoviePackage))
                    //Add Correct description summaries 
                    .Bind(() => _metadataMapper.InsertDescriptionData(
                        entry.GraceNoteData.MovieEpisodeProgramData.descriptions, entry.AdiData))
                    //Insert the Year data based on air date
                    .Bind(() => _metadataMapper.InsertYearData(
                        entry.GraceNoteData.MovieEpisodeProgramData.origAirDate,
                        entry.GraceNoteData.MovieEpisodeProgramData?.movieInfo,
                        entry.AdiData))
                    //Insert Program Genres and Genre Id's
                    .Bind(() => _metadataMapper.InsertGenreData(entry.GraceNoteData.EnrichmentDataLists, entry.AdiData))
                    //Insert required IMDB Data
                    .Bind(() => _metadataMapper.InsertIdmbData(
                        entry.GraceNoteData.ExternalLinks(),
                        entry.AdiData,
                        entry.GraceNoteData.HasMovieInfo()));

                return result;
            }
            catch (Exception ex)
            {
                return _logger.LogErrorResult(ex, "SetAdiMovieEpisodeMetadata - Error Setting Title Metadata");
            }
        }

        protected Task<Result<PackageEntry>> GetGracenoteMovieEpisodeData(PackageEntry entry)
        {
            //Layer1Data
            return _graceNote.RetrieveAndAddProgramData(entry);
        }

        protected Result SetAdiMovieMetadata(PackageEntry packageEntry)
        {
            try
            {
                _logger.LogInformation("Setting ADI Content Metadata");
                var paid = packageEntry.TitlPaIdValue.Replace("TITL", "ASST");
                return packageEntry.AdiData.AddAssetMetadataApp_DataNode(
                        paid,
                        "Content_CheckSum",
                        packageEntry.MovieChecksum
                    )
                    .Bind(() => packageEntry.AdiData.AddAssetMetadataApp_DataNode(
                        paid,
                        "Content_FileSize",
                        packageEntry.MovieFileSize
                    ))
                    .Bind(() => packageEntry.AdiData.TrySetAdiAssetContentField(
                        "movie",
                        packageEntry.PrimaryAsset.Name));
            }
            catch (Exception ex)
            {
                return _logger.LogErrorResult(ex, "Error Setting Title Movie metadata");
            }
        }

        protected Result CheckAndAddPreviewData(PackageEntry entry)
        {
            try
            {
                if (!entry.HasPreviewAssets)
                {
                    return Result.Success();
                }

                var previewPaid = entry.TitlPaIdValue.Replace("TITL", "PREV");
                var checksum = _fileSystemWorker.GetFileHash(entry.PreviewAsset.FullName);
                var previewSize = _fileSystemWorker.GetFileSize(entry.PreviewAsset.FullName);

                return UpdateAdiDataTable(entry, previewPaid, previewSize, checksum)
                    .Bind(() => UpdateAdiAssetData(entry, previewPaid, previewSize, checksum));
            }
            catch (Exception ex)
            {
                return _logger.LogErrorResult(ex, "Error Setting Preview metadata");
            }
        }

        protected Result UpdateAdiDataTable(PackageEntry entry, string previewPaId, string previewSize,
            string checksum)
        {
            try
            {
                entry.AdiData.Entity.PreviewFile = entry.PreviewAsset.Name;
                entry.AdiData.Entity.PreviewFilePaid = previewPaId;
                entry.AdiData.Entity.PreviewFileChecksum = checksum;
                entry.AdiData.Entity.PreviewFileSize = previewSize;
                _context.Adi_Data.Update(entry.AdiData.Entity);
                _context.SaveChanges();
                return Result.Success();
            }
            catch (Exception ex)
            {
                return _logger.LogErrorResult(ex, "Failed to update Adi_Data table.");
            }
        }

        protected Result UpdateAdiAssetData(PackageEntry entry, string previewpaid, string previewSize,
            string checksum)
        {
            return entry.AdiData.AddAssetMetadataApp_DataNode(
                    previewpaid,
                    "Content_CheckSum",
                    checksum
                )
                .Bind(() => entry.AdiData.AddAssetMetadataApp_DataNode(
                    previewpaid,
                    "Content_FileSize",
                    previewSize
                ))
                .Bind(() => entry.AdiData.TrySetAdiAssetContentField(
                    "preview",
                    entry.PreviewAsset.Name));
        }


        protected void SetPreviewAsset(PackageEntry entry)
        {
            entry.AdiData.TrySetAdiAssetContentField("preview", entry.ArchiveInfo.ExtractedPreview.Name);
            entry.PreviewAsset = entry.ArchiveInfo.ExtractedPreview;
        }

        protected Result ExtractPackageMedia(PackageEntry entry)
        {
            _logger.LogInformation("Extracting Media from Package");

            //Extract remaining items from package
            return _zipWorker.ExtractArchive(entry.ArchiveInfo, entry.IsPackageAnUpdate)
                .Tap(() => _fileSystemWorker.CheckAndDeletePosterAssets(entry.ArchiveInfo.WorkingDirectory));
        }

        protected Result SaveAdiToDatabase(PackageEntry entry)
        {
            try
            {
                var isMapped = _context.Adi_Data.Any(p => p.TitlPaid == entry.TitlPaIdValue);
                if (!isMapped)
                {
                    _logger.LogInformation("Seeding Adi Data to the database");
                    var adiData = new Adi_Data
                    {
                        IngestUUID = entry.IngestUuid,
                        TitlPaid = entry.TitlPaIdValue,
                        OriginalAdi = _fileSystemWorker.ReadAdiAsString(entry.ArchiveInfo.ExtractedAdiFile.FullName),
                        VersionMajor = entry.AdiData.GetVersionMajor(),
                        VersionMinor = entry.AdiData.GetVersionMinor(),
                        ProviderId = entry.AdiData.GetProviderId(),
                        TmsId = entry.GraceNoteData.GraceNoteTmsId,
                        Licensing_Window_End = entry.AdiData.GetLicenceEndDate().ToString(CultureInfo.InvariantCulture),
                        ProcessedDateTime = _systemClock.UtcNow.UtcDateTime,
                        ContentTsFile = entry.ArchiveInfo.ExtractedMovieAsset.Name,
                        ContentTsFilePaid = entry.AdiData.GetAssetPaid("movie"),
                        ContentTsFileSize = entry.MovieFileSize,
                        ContentTsFileChecksum = entry.MovieChecksum,
                        PreviewFile = entry.HasPreviewAssets ? entry.ArchiveInfo.ExtractedPreview.Name : string.Empty,
                        PreviewFilePaid = entry.HasPreviewAssets ? entry.AdiData.GetAssetPaid("preview") : string.Empty,
                        PreviewFileSize = entry.PreviewFileSize,
                        PreviewFileChecksum = entry.PreviewFileChecksum
                    };

                    _context.Adi_Data.Add(adiData);
                    _context.SaveChanges();
                    _logger.LogInformation($"Adi data seeded to the database with Id: {adiData.Id}");

                    entry.AdiData.Entity = adiData;
                    return Result.Success();
                }

                var message = $"Failed to seed data - data exists for PAID: {entry.TitlPaIdValue}. Failing Ingest.";
                _logger.LogError(message);
                return Result.Failure(message);
            }
            catch (Exception ex)
            {
                return _logger.LogErrorResult(ex, "Error during seed of Adi Data");
            }
        }

        protected Result SetAdiPackageVars(PackageEntry entry)
        {
            entry.MovieFileSize =
                _fileSystemWorker.GetFileSize(entry.ArchiveInfo.ExtractedMovieAsset.FullName);
            entry.MovieChecksum =
                _fileSystemWorker.GetFileHash(entry.ArchiveInfo.ExtractedMovieAsset.FullName);

            entry.PreviewFileSize = entry.ArchiveInfo.HasPreviewAssets
                ? _fileSystemWorker.GetFileSize(entry.ArchiveInfo.ExtractedPreview.FullName)
                : string.Empty;

            entry.PreviewFileChecksum = entry.ArchiveInfo.HasPreviewAssets
                ? _fileSystemWorker.GetFileHash(entry.ArchiveInfo.ExtractedPreview.FullName)
                : string.Empty;

            return Result.Success();
        }

        protected Result SetMovieAsset(PackageEntry entry)
        {
            entry.AdiData.TrySetAdiAssetContentField("movie", entry.ArchiveInfo.ExtractedMovieAsset.Name);
            entry.PrimaryAsset = entry.ArchiveInfo.ExtractedMovieAsset;
            return Result.Success();
        }

        protected Result<string> PackageEnrichedAsset(PackageEntry entry)
        {
            try
            {
                var deliveryPackage = Path.Combine(_options.TempWorkingDirectory,
                    $"{Path.GetFileNameWithoutExtension(entry.ArchiveInfo.Archive.Name)}.zip");

                return _zipWorker.CreateArchive(entry.ArchiveInfo.WorkingDirectory.FullName,
                        deliveryPackage)
                    .Map(() => deliveryPackage);
            }
            catch (Exception ex)
            {
                var message = "Error Creating Final Package";
                _logger.LogError(ex, message);
                return Result.Failure<string>(message);
            }
        }

        protected Result SaveAdiFile(PackageEntry entry)
        {
            try
            {
                var outputAdi = Path.Combine(entry.ArchiveInfo.WorkingDirectory.FullName, "ADI.xml");
                _fileSystemWorker.SaveAdiFile(entry.ArchiveInfo.WorkingDirectory, entry.AdiData.Adi);
                var adiString = _fileSystemWorker.ReadAdiAsString(outputAdi);

                if (entry.AdiData.Entity is null)
                {
                    entry.AdiData.Entity = _context.Adi_Data.First(i => i.IngestUUID.Equals(entry.IngestUuid));
                }

                if (entry.IsPackageAnUpdate)
                {
                    entry.AdiData.Entity.UpdateAdi = adiString;
                    entry.AdiData.Entity.Update_DateTime = DateTime.Now;
                }
                else
                {
                    entry.AdiData.Entity.EnrichedAdi = adiString;
                    entry.AdiData.Entity.Enrichment_DateTime = DateTime.Now;
                }

                _context.Adi_Data.Update(entry.AdiData.Entity);
                _context.SaveChanges();
                return Result.Success();
            }
            catch (Exception ex)
            {
                return _logger.LogErrorResult(ex, "Error Saving Enriched ADI");
            }
        }

        protected Result UpdateAdiVersions(PackageEntry entry)
        {
            try
            {
                var versionMajor = entry.AdiData.Adi.Metadata.AMS.Version_Major;
                var versionMinor = entry.AdiData.Adi.Metadata.AMS.Version_Minor;

                //Update all version major values to correct value.
                var updateResult = UpdateAllVersionMajorValues(entry, versionMajor)
                    .Bind(() => UpdateAllVersionMinorValues(entry, versionMinor));

                if (updateResult.IsFailure)
                {
                    return updateResult;
                }

                _context.Adi_Data.Update(entry.AdiData.Entity);
                _context.SaveChanges();
                _logger.LogInformation("Adi data table updated with correct version data.");

                return Result.Success();
            }
            catch (Exception ex)
            {
                return _logger.LogErrorResult(ex, "Error Saving Enriched ADI");
            }
        }

        protected Result UpdateAllVersionMinorValues(PackageEntry entry, int minorVersion)
        {
            if (minorVersion == 0)
            {
                return Result.Success();
            }

            try
            {
                //set main ams version minor
                entry.AdiData.Adi.Metadata.AMS.Version_Minor = minorVersion;
                //set titl data ams version minor
                entry.AdiData.Adi.Metadata.AMS.Version_Minor = minorVersion;
                //iterate any asset sections and update the version minor
                foreach (var item in entry.AdiData.Adi.Asset.Asset.ToList()
                    .Where(item => item.Metadata.AMS.Version_Minor != minorVersion))
                {
                    item.Metadata.AMS.Version_Minor = minorVersion;
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                return _logger.LogErrorResult(ex, "[UpdateAllVersionMinorValues] Error during update of version Minor" +
                                                  $": {ex.Message}");
            }
        }

        protected Result UpdateAllVersionMajorValues(PackageEntry entry, int majorVersion)
        {
            try
            {
                foreach (var item in entry.AdiData.Adi.Asset.Asset
                    .Where(item => item.Metadata.AMS.Version_Major != majorVersion))
                {
                    item.Metadata.AMS.Version_Major = majorVersion;
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                return _logger.LogErrorResult(ex, "[UpdateAllVersionMajorValues] Error during update of version Major" +
                                                  $": {ex.Message}");
            }
        }

        protected Result FinalisePackageData(PackageEntry entry)
        {
            return _metadataMapper.InsertProgramLayerData(
                    entry.GraceNoteData.GraceNoteTmsId,
                    entry.GraceNoteData.MovieEpisodeProgramData?.rootId,
                    entry.GraceNoteData.ShowSeriesSeasonProgramData?.rootId,
                    entry.AdiData)
                .Bind(() => _metadataMapper.CheckAndAddBlockPlatformData(entry.AdiData))
                .BindIf(entry.IsQamAsset && entry.IsPackageAnUpdate,
                    () => _metadataMapper.SetQamUpdateContent(entry.AdiData, entry.IsPackageAnUpdate))
                .Bind(() => SetDbAssetData(entry))
                .Bind(() => SetTrackingData(entry));
        }

        protected Result SetDbAssetData(PackageEntry entry)
        {
            try
            {
                var enrichedChecksum =
                    entry.AdiData.Adi.Asset.Asset.FirstOrDefault(a =>
                        a.Metadata.AMS.Asset_Class == "movie");
                entry.AdiData.Entity.ContentTsFileChecksum = enrichedChecksum?.Metadata.App_Data
                    .FirstOrDefault(c => c.Name.ToLower() == "content_checksum")
                    ?.Value;

                entry.AdiData.Entity.ContentTsFileSize = enrichedChecksum?.Metadata.App_Data
                    .FirstOrDefault(c => c.Name.ToLower() == "content_filesize")?.Value;

                _context.Adi_Data.Update(entry.AdiData.Entity);
                _context.SaveChanges();
                return Result.Success();
            }
            catch (Exception ex)
            {
                return _logger.LogErrorResult(ex, "Error saving ADI data to the DB.");
            }
        }

        protected Result SetTrackingData(PackageEntry entry)
        {
            _logger.LogInformation("Setting Updates Tracking data.");
            var result = AddOrUpdateMappingTrackingData(entry)
                .Bind(() => AddOrUpdateLayer1TrackingData(entry))
                .Bind(() => AddOrUpdateLayer2TrackingData(entry));

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully Set Tracking Data.");
            }
            else
            {
                _logger.LogError("Failed to Set Tracking Data Check previous log entries.");
            }

            return result;
        }

        protected Result AddOrUpdateMappingTrackingData(PackageEntry entry)
        {
            try
            {
                var mapTracking = _context.MappingsUpdateTracking.FirstOrDefault(m => m.IngestUUID == entry.IngestUuid);
                var updateId = entry.GraceNoteData.MappingData.programMappings.programMapping.FirstOrDefault()
                    ?.updateId;

                if (mapTracking == null)
                {
                    mapTracking = new MappingsUpdateTracking
                    {
                        IngestUUID = entry.IngestUuid,
                        GN_ProviderId = entry.GraceNoteData.Entity.GN_ProviderId,
                        Mapping_MaxUpdateId = updateId,
                        Mapping_NextUpdateId = updateId,
                        Mapping_RootId = entry.GraceNoteData.GraceNoteRootId,
                        Mapping_UpdateDate = DateTime.Now,
                        Mapping_UpdateId = updateId,
                        UpdatesChecked = DateTime.Now,
                        RequiresEnrichment = false
                    };

                    _context.MappingsUpdateTracking.Add(mapTracking);
                }
                else
                {
                    mapTracking.GN_ProviderId = entry.GraceNoteData.Entity.GN_ProviderId;
                    mapTracking.Mapping_UpdateId = updateId;
                    mapTracking.Mapping_RootId = entry.GraceNoteData.GraceNoteRootId;
                    mapTracking.UpdatesChecked = DateTime.Now;
                    mapTracking.RequiresEnrichment = false;

                    _context.MappingsUpdateTracking.Update(mapTracking);
                }

                _context.SaveChanges();
                return Result.Success();
            }
            catch (Exception ex)
            {
                return _logger.LogErrorResult(ex, "Failed to Set Mapping Tracking data.");
            }
        }

        protected Result AddOrUpdateLayer1TrackingData(PackageEntry entry)
        {
            try
            {
                //layer1 = CoreProgramData
                var layer1Tracking =
                    _context.Layer1UpdateTracking.FirstOrDefault(l => l.IngestUUID == entry.IngestUuid);
                var updateId = entry.GraceNoteData.MovieEpisodeProgramData?.updateId ??
                               entry.GraceNoteData.GraceNoteUpdateId;

                if (layer1Tracking == null)
                {
                    layer1Tracking = new Layer1UpdateTracking
                    {
                        IngestUUID = entry.IngestUuid,
                        GN_Paid = entry.GraceNoteData.Entity.GN_Paid,
                        GN_TMSID = entry.GraceNoteData.MovieEpisodeProgramData?.TMSId,
                        Layer1_UpdateId = updateId,
                        Layer1_UpdateDate = DateTime.Now,
                        Layer1_NextUpdateId = updateId,
                        Layer1_MaxUpdateId = updateId,
                        Layer1_RootId = entry.GraceNoteData.MovieEpisodeProgramData?.rootId ??
                                        entry.GraceNoteData.GraceNoteRootId,
                        UpdatesChecked = DateTime.Now,
                        RequiresEnrichment = false
                    };

                    _context.Layer1UpdateTracking.Add(layer1Tracking);
                    _context.SaveChanges();
                }
                else
                {
                    layer1Tracking.GN_Paid = entry.GraceNoteData.Entity.GN_Paid;
                    layer1Tracking.GN_TMSID = entry.GraceNoteData.MovieEpisodeProgramData?.TMSId;
                    layer1Tracking.Layer1_UpdateId = updateId;
                    layer1Tracking.Layer1_RootId = entry.GraceNoteData.MovieEpisodeProgramData?.rootId ??
                                                   entry.GraceNoteData.GraceNoteRootId;
                    layer1Tracking.UpdatesChecked = DateTime.Now;
                    layer1Tracking.RequiresEnrichment = false;

                    _context.Layer1UpdateTracking.Update(layer1Tracking);
                    _context.SaveChanges();
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                return _logger.LogErrorResult(ex, "Failed to Set Layer1 Tracking data.");
            }
        }

        protected Result AddOrUpdateLayer2TrackingData(PackageEntry entry)
        {
            try
            {
                //layer2 = CoreSeriesData
                var layer2Tracking =
                    _context.Layer2UpdateTracking.FirstOrDefault(l => l.IngestUUID == entry.IngestUuid);
                var updateId = entry.GraceNoteData.ShowSeriesSeasonProgramData?.updateId ??
                               entry.GraceNoteData.GraceNoteUpdateId;
                if (layer2Tracking == null)
                {
                    layer2Tracking = new Layer2UpdateTracking
                    {
                        IngestUUID = entry.IngestUuid,
                        GN_Paid = entry.GraceNoteData.Entity.GN_Paid,
                        GN_connectorId = entry.GraceNoteData.ShowSeriesSeasonProgramData?.connectorId ??
                                         entry.GraceNoteData.GraceNoteConnectorId,
                        Layer2_UpdateId = updateId,
                        Layer2_UpdateDate = DateTime.Now,
                        Layer2_NextUpdateId = updateId,
                        Layer2_MaxUpdateId = updateId,
                        Layer2_RootId = entry.GraceNoteData.ShowSeriesSeasonProgramData?.rootId ??
                                        entry.GraceNoteData.GraceNoteRootId,
                        UpdatesChecked = DateTime.Now,
                        RequiresEnrichment = false
                    };

                    _context.Layer2UpdateTracking.Add(layer2Tracking);
                }
                else
                {
                    layer2Tracking.GN_Paid = entry.GraceNoteData.Entity.GN_Paid;
                    layer2Tracking.GN_connectorId = entry.GraceNoteData.ShowSeriesSeasonProgramData?.connectorId ??
                                                    entry.GraceNoteData.GraceNoteConnectorId;
                    layer2Tracking.Layer2_UpdateId = updateId ?? entry.GraceNoteData.GraceNoteUpdateId;
                    layer2Tracking.Layer2_RootId = entry.GraceNoteData.ShowSeriesSeasonProgramData?.rootId ??
                                                   entry.GraceNoteData.GraceNoteRootId;
                    layer2Tracking.UpdatesChecked = DateTime.Now;
                    layer2Tracking.RequiresEnrichment = false;

                    _context.Layer2UpdateTracking.Update(layer2Tracking);
                }

                _context.SaveChanges();
                return Result.Success();
            }
            catch (Exception ex)
            {
                return _logger.LogErrorResult(ex, "Failed to Set Layer1 Tracking data.");
            }
        }

        protected Result RemovedDerivedFromAsset(PackageEntry entry)
        {
            try
            {
                //<App_Data App="VOD" Name="DeriveFromAsset" Value="ASST0000000001506105" />
                foreach (var asset in from asset in entry.AdiData.Adi.Asset.Asset
                    let dfa = asset.Metadata.App_Data.FirstOrDefault(d =>
                        d.Name.ToLower() == "derivefromasset"
                    )
                    where dfa != null
                    select asset)
                {
                    _logger.LogInformation("Removing DeriveFromAsset section from ADI.xml");
                    entry.AdiData.Adi.Asset.Asset.Remove(asset);
                    break;
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                return _logger.LogErrorResult(ex, "Error removing DerivedFromAsset");
            }
        }
    }
}