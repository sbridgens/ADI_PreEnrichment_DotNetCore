using System;
using System.Linq;
using System.Threading.Tasks;
using Application.Configuration;
using Application.DataAccess.Persistence.Contracts;
using Application.Models;
using CSharpFunctionalExtensions;
using Domain.Entities;
using Domain.Schema.GNMappingSchema;
using Domain.Schema.GNProgramSchema.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.ApiManager
{
    public class GraceNoteMetadataProvider : IGraceNoteMetadataProvider
    {
        private readonly IApplicationDbContext _context;
        private readonly IGraceNoteApi _graceNoteApi;
        private readonly ILogger<GraceNoteMetadataProvider> _logger;
        private readonly EnrichmentSettings _options;
        private readonly IProgramTypeLookupStore _programTypeLookupStore;

        public GraceNoteMetadataProvider(IGraceNoteApi graceNoteApi,
            IApplicationDbContext context,
            IProgramTypeLookupStore programTypeLookupStore,
            IOptions<EnrichmentSettings> options,
            ILogger<GraceNoteMetadataProvider> logger)
        {
            _graceNoteApi = graceNoteApi;
            _context = context;
            _programTypeLookupStore = programTypeLookupStore;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<Result<PackageEntry>> RetrieveAndAddProgramMapping(PackageEntry entry)
        {
            var response = await _graceNoteApi.GetMappingData(entry.OnApiProviderId);
            if (response.IsFailure)
            {
                entry.FailedToMap = true;
                _logger.LogError(response.Error);
                return Result.Failure<PackageEntry>(
                    $"Failed to retrieve data from GraceNote API for Id {entry.GraceNoteData.GraceNoteTmsId}");
            }

            entry.GraceNoteData.MappingData = response.Value;
            if (!IsOnApiMappingDataExists(entry))
            {
                _logger.LogWarning("Processing Stopped as mapping data is not ready, " +
                                   $"package will be retried for {_options.FailedToMap_Max_Retry_Days} days before failing!");
                return Result.Failure<PackageEntry>("Mapping not available");
            }

            if (!SetGracenoteMappingData(entry))
            {
                return Result.Failure<PackageEntry>("Mapping not available");
            }

            _logger.LogInformation($"Mapping data with TmsID: {entry.GraceNoteData.GraceNoteTmsId} parsed successfully, continuing package processing.");
            return Result.Success(entry);
        }

        public async Task<Result<PackageEntry>> RetrieveAndAddProgramData(PackageEntry entry)
        {
            var response = await _graceNoteApi.GetProgramData(entry.GraceNoteData.GraceNoteTmsId);
            if (response.IsFailure)
            {
                _logger.LogError(response.Error);
                //TODO: add retry policy
                return Result.Failure<PackageEntry>(
                    $"Failed to retrieve data from GraceNote API for Id {entry.GraceNoteData.GraceNoteTmsId}");
            }

            entry.GraceNoteData.CoreProgramData = response.Value;
            entry.GraceNoteData.MovieEpisodeProgramData = response.Value.programs.FirstOrDefault();
            _logger.LogInformation("Successfully serialized Gracenote Episode/Movie data");
            UpdateLayer1DbData(entry);
            _programTypeLookupStore.SetProgramType(entry);
            //set default vars for workflow
            InitialiseAndSeedObjectLists(entry, entry.GraceNoteData.MovieEpisodeProgramData.GetSeasonId().ToString());
            return Result.Success(entry);
        }
        
        public async Task<Result<PackageEntry>> RetrieveAndAddSeriesSeasonSpecialsData(PackageEntry entry)
        {
            var response = await _graceNoteApi.GetProgramData(entry.GraceNoteData.GraceNoteConnectorId);
            if (response.IsFailure)
            {
                _logger.LogError(response.Error);
                return Result.Failure<PackageEntry>(
                    $"Failed to retrieve data from GraceNote API for Id {entry.GraceNoteData.GraceNoteTmsId}");
            }

            entry.GraceNoteData.CoreSeriesData = response.Value;
            entry.GraceNoteData.ShowSeriesSeasonProgramData = response.Value.programs.FirstOrDefault();
            _logger.LogInformation("Successfully serialized Gracenote Series/Season/Specials data");

            return Result.Success(entry);
        }

        public Result UpdateGnMappingData(PackageEntry entry)
        {
            try
            {
                entry.GraceNoteData.Entity =
                    _context.GN_Mapping_Data.FirstOrDefault(i => i.IngestUUID.Equals(entry.IngestUuid));

                if (entry.GraceNoteData.Entity == null)
                {
                    return Result.Failure("Failed to update the GN Mapping table no Data received for" +
                                          $" IngestUuid:{entry.IngestUuid}! Is this Ingest a genuine Update?");
                }

                return CheckIfTmsIdChanged(entry)
                    .Bind(() => UpdateGnMappingDbData(entry));
            }
            catch (Exception ex)
            {
                var message = "Error updating db Mapping data";
                _logger.LogError(ex, message);
                return Result.Failure(message);
            }
        }

        public Result SeedGnMappingData(PackageEntry entry)
        {
            try
            {
                var mapData = GetGnMappingData(entry);
                entry.GraceNoteData.GnMappingPaid = mapData.link.Where(i => i.idType.Equals("PAID"))
                    .Select(r => r.Value)
                    .FirstOrDefault();


                //secondary check
                if (mapData.status == GnOnApiProgramMappingSchema.onProgramMappingsProgramMappingStatus.Mapped)
                {
                    _logger.LogInformation(
                        $"Asset Mapping status: {mapData.status}, Catalog Name: {mapData.catalogName}");

                    entry.GraceNoteData.GraceNoteUpdateId = mapData.updateId;

                    var data = new GN_Mapping_Data
                    {
                        IngestUUID = entry.IngestUuid,
                        GN_TMSID = entry.GraceNoteData.GraceNoteTmsId,
                        GN_Paid = mapData.link.Where(i => i.idType.Equals("PAID"))
                            .Select(r => r.Value)
                            .FirstOrDefault(),

                        GN_RootID = mapData.id.Where(t => t.type.Equals("rootId"))
                            .Select(r => r.Value)
                            .FirstOrDefault(),

                        GN_Status = mapData.status.ToString(),
                        GN_ProviderId = mapData.link.Where(i => i.idType.Equals("ProviderId"))
                            .Select(r => r.Value)
                            .FirstOrDefault(),

                        GN_Pid = mapData.link.Where(i => i.idType.Equals("PID"))
                            .Select(r => r.Value)
                            .FirstOrDefault(),

                        GN_programMappingId = mapData.programMappingId,
                        GN_creationDate = mapData.creationDate != null
                            ? Convert.ToDateTime(mapData.creationDate)
                            : DateTime.Now,
                        GN_updateId = mapData.updateId,
                        GN_Availability_Start = GetAvailability("start", mapData),
                        GN_Availability_End = GetAvailability("end", mapData)
                    };

                    var created = _context.GN_Mapping_Data.Add(data);
                    _context.SaveChanges();
                    entry.GraceNoteData.Entity = created.Entity;
                    _logger.LogInformation(
                        $"Gracenote Mapping data seeded to the database with Row Id: {entry.GraceNoteData.Entity.Id}");
                    return Result.Success();
                }

                _logger.LogInformation(
                    $"Package {entry.TitlPaIdValue} is not mapped, Status returned: {mapData.status}, Catalog Name: {mapData.catalogName}");
                entry.FailedToMap = true;
                return Result.Failure(
                    $"Package {entry.TitlPaIdValue} is not mapped, Status returned: {mapData.status}, Catalog Name: {mapData.catalogName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "SeedGnMappingData",
                    "Error Seeding Mapping data", ex);
                return Result.Failure(ex.Message);
            }
        }

        private void InitialiseAndSeedObjectLists(PackageEntry entry, string seasonId)
        {
            //Instantiate List Entities
            entry.GraceNoteData.EnrichmentDataLists = new EnrichmentDataLists();
            entry.GraceNoteData.EnrichmentDataLists.UpdateListData(entry.GraceNoteData.MovieEpisodeProgramData,
                seasonId);
        }

        private void UpdateLayer1DbData(PackageEntry entry)
        {
            entry.GraceNoteData.GraceNoteConnectorId = entry.GraceNoteData.MovieEpisodeProgramData.connectorId;
            entry.GraceNoteData.Entity.GN_connectorId = entry.GraceNoteData.GraceNoteConnectorId;
            _context.GN_Mapping_Data.Update(entry.GraceNoteData.Entity);
            _context.SaveChanges();
            _logger.LogInformation("[GetGracenoteProgramEpisodeData] Successfully updated GN Mapping table.");
        }

        private bool IsOnApiMappingDataExists(PackageEntry entry)
        {
            //Fail if no mapping data detected
            if (entry.GraceNoteData.MappingData.programMappings.programMapping != null)
            {
                return true;
            }

            entry.FailedToMap = true;
            return false;
        }

        private bool SetGracenoteMappingData(PackageEntry entry)
        {
            //parse mapping data and get mapped status
            var gnMappingData =
                entry.GraceNoteData.MappingData.programMappings.programMapping.FirstOrDefault
                (p => p.link.Any
                    (
                        i => i.Value == entry.OnApiProviderId &&
                             string.Equals
                             (
                                 p.status.ToString(),
                                 GnOnApiProgramMappingSchema.onProgramMappingsProgramMappingStatus.Mapped
                                     .ToString(),
                                 StringComparison.CurrentCultureIgnoreCase)
                    )
                );

            if (gnMappingData == null)
            {
                _logger.LogWarning(
                    $"Processing Stopped as mapping data is not ready, package will be retried for {_options.FailedToMap_Max_Retry_Days} days before failing!");
                entry.FailedToMap = true;
                return false;
            }

            entry.GraceNoteData.GraceNoteMappingData = gnMappingData;

            //extract tmsid
            entry.GraceNoteData.GraceNoteTmsId = gnMappingData
                .id.Where(t =>
                {
                    if (t == null)
                    {
                        throw new ArgumentNullException(nameof(t));
                    }

                    return t.type.Equals("TMSId");
                })
                .Select(r => r?.Value)
                .FirstOrDefault();

            //extract rootid
            entry.GraceNoteData.GraceNoteRootId = gnMappingData
                .id.Where(t =>
                {
                    if (t == null)
                    {
                        throw new ArgumentNullException(nameof(t));
                    }

                    return t.type.Equals("rootId");
                })
                .Select(r => r?.Value)
                .FirstOrDefault();

            return true;
        }

        private GnOnApiProgramMappingSchema.onProgramMappingsProgramMapping GetGnMappingData(PackageEntry entry)
        {
            return entry.GraceNoteData.MappingData
                .programMappings
                .programMapping
                .FirstOrDefault(m =>
                    m.status == GnOnApiProgramMappingSchema.onProgramMappingsProgramMappingStatus.Mapped);
        }

        private static DateTime? GetAvailability(string typeRequired,
            GnOnApiProgramMappingSchema.onProgramMappingsProgramMapping mapdata)
        {
            DateTime? availableDateTime = null;


            switch (typeRequired)
            {
                case "start":
                {
                    if ((mapdata.availability?.start != null) & (mapdata.availability?.start.Year != 1))
                    {
                        availableDateTime = Convert.ToDateTime(mapdata.availability?.start);
                    }

                    break;
                }
                case "end":
                {
                    if ((mapdata.availability?.end != null) & (mapdata.availability?.end.Year != 1))
                    {
                        availableDateTime = Convert.ToDateTime(mapdata.availability?.end);
                    }

                    break;
                }
            }

            return availableDateTime;
        }

        private Result CheckIfTmsIdChanged(PackageEntry entry)
        {
            if (entry.GraceNoteData.Entity.GN_TMSID == entry.GraceNoteData.GraceNoteTmsId)
            {
                return Result.Success();
            }

            try
            {
                _logger.LogInformation("TMSID Mismatch updating ADI_Data and Layer1UpdateTracking Table with new value.");
                entry.GraceNoteData.Entity.GN_TMSID = entry.GraceNoteData.GraceNoteTmsId;
                //Update All TMSID's in the Layer1 tracking table with the tmsid update
                var layer1Data =
                    _context.Layer1UpdateTracking.Where(t => t.GN_TMSID == entry.GraceNoteData.Entity.GN_TMSID).ToList();

                foreach (var l1Item in layer1Data.ToList())
                {
                    _logger.LogInformation(
                        $"Updating TMSID in the Layer1 table with ingestUUID: {l1Item.IngestUUID} with new TmsID: {entry.GraceNoteData.GraceNoteTmsId}");
                    l1Item.GN_TMSID = entry.GraceNoteData.GraceNoteTmsId;
                    _context.Layer1UpdateTracking.Update(l1Item);
                    _context.SaveChanges();
                }
                
                return Result.Success();
            }
            catch (Exception e)
            {
                var message = "Error while updating TmsId in the database.";
                _logger.LogError(e,message);
                return Result.Failure(message);
            }
        }

        private Result UpdateGnMappingDbData(PackageEntry entry)
        {

            try
            {
                _logger.LogInformation("Updating GN_Mapping_Data table with new gracenote mapping data.");
                var mapData = GetGnMappingData(entry);
                entry.GraceNoteData.Entity.GN_Availability_Start = GetAvailability("start", mapData);
                entry.GraceNoteData.Entity.GN_Availability_End = GetAvailability("end", mapData);
                entry.GraceNoteData.Entity.GN_updateId = mapData?.updateId;
                _context.GN_Mapping_Data.Update(entry.GraceNoteData.Entity);
                _context.SaveChanges();
                return Result.Success();
            }
            catch (Exception e)
            {
                var message = "Error while updating GN_Mapping_Data table with new gracenote mapping data.";
                _logger.LogError(e, message);
                return Result.Failure(message);
            }
        }
    }
}