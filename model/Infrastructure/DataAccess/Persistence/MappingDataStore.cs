using System;
using System.Collections.Generic;
using System.Linq;
using Application.DataAccess.Persistence.Contracts;
using Application.Extensions;
using CSharpFunctionalExtensions;
using Domain.Entities;
using Domain.Schema.GNProgramSchema;
using Microsoft.Extensions.Logging;

namespace Infrastructure.DataAccess.Persistence
{
    public class MappingDataStore : IGnMappingDataStore
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<MappingDataStore> _logger;

        public MappingDataStore(IApplicationDbContext context, ILogger<MappingDataStore> logger)
        {
            _context = context;
            _logger = logger;
        }

        public bool CleanMappingDataWithNoAdi()
        {
            try
            {
                var adiData = _context.Adi_Data;

                var mappedNoAdi = _context.GN_Mapping_Data.Where(map =>
                    !adiData.Any(adata =>
                        StringExtensions.GetPaidLastValue(adata.TitlPaid) ==
                        StringExtensions.GetPaidLastValue(map.GN_Paid))
                ).ToList();

                if (mappedNoAdi.FirstOrDefault() != null)
                {
                    _context.GN_Mapping_Data.RemoveRange(mappedNoAdi);
                    _context.SaveChanges();
                }
                else
                {
                    _logger.LogInformation("No orphaned GN mappings found.");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"General Exception during database connection: {ex.Message}");
                return false;
            }
        }

        public Result AddGraceNoteProgramData(Guid ingestGuid, string seriesTitle, string episodeTitle,
            GnApiProgramsSchema.programsProgram programData)
        {
            _logger.LogInformation("Updating Gracenote database Mapping table with Program Data");

            var gnMappingData = ReturnMapData(ingestGuid);
            if (gnMappingData == null)
            {
                return Result.Failure("No mapping data found in database.");
            }

            gnMappingData.GN_SeasonId = Convert.ToInt32(programData?.seasonId);
            gnMappingData.GN_SeasonNumber = Convert.ToInt32(programData?.episodeInfo?.season);
            gnMappingData.GN_SeriesId = Convert.ToInt32(programData?.seriesId);
            gnMappingData.GN_EpisodeNumber = Convert.ToInt32(programData?.episodeInfo?.number);
            gnMappingData.GN_EpisodeTitle = episodeTitle;
            gnMappingData.GN_SeriesTitle = seriesTitle;
            _context.GN_Mapping_Data.Update(gnMappingData);
            _context.SaveChanges();

            _logger.LogInformation("GN Mapping database updated," +
                                   $" where GN_Paid: {gnMappingData.GN_Paid} & Row ID: {gnMappingData.Id}");


            return Result.Success();
        }

        public Dictionary<string, string> ReturnDbImagesForAsset(string paidValue, int rowId, bool isTrackerService)
        {
            var dbImages = new Dictionary<string, string>();
            if (!isTrackerService)
            {
                _logger.LogDebug($"Retrieving image data from the db where GNPAID = {paidValue}");
            }

            try
            {
                var imgList = _context.GN_Mapping_Data.Where(i => (i.Id == rowId) & !string.IsNullOrEmpty(i.GN_Images))
                    .Select(i => i.GN_Images)
                    .FirstOrDefault()
                    ?.Split(',')
                    .Select(k => k.Trim().Split(':'))
                    .ToList();

                if (imgList == null)
                {
                    return dbImages;
                }

                foreach (var kv in imgList.Where(kv => !dbImages.ContainsKey(kv[0])))
                {
                    dbImages.Add(kv[0], kv[1]);
                }

                _context.SaveChanges();

                return dbImages;
            }
            catch (Exception gdbiEx)
            {
                _logger.LogError(
                    $"Error obtaining DB Images for GN PAID: {paidValue}, ERROR = {gdbiEx.Message}");
                if (gdbiEx.InnerException != null)
                {
                    _logger.LogError($"Inner Exception: {gdbiEx.InnerException.Message}");
                }

                _logger.LogInformation("Continuing with workflow!");

                return dbImages;
            }
        }

        public GN_Mapping_Data ReturnMapData(Guid ingestGuid)
        {
            return _context.GN_Mapping_Data.FirstOrDefault(i => i.IngestUUID.Equals(ingestGuid));
        }
    }
}