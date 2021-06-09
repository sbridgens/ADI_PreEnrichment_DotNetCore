using System.Linq;
using Application.DataAccess.Persistence.Contracts;
using Application.Models;
using Application.Validation.Contracts;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Validation
{
    public class PackageValidator : IPackageValidator
    {
        private readonly IApplicationDbContext _context;
        private readonly IVersionChecker _versionChecker;
        private readonly ILogger<PackageValidator> _logger;

        public PackageValidator(IVersionChecker versionChecker, 
            IApplicationDbContext context,
            ILogger<PackageValidator> logger)
        {
            _versionChecker = versionChecker;
            _context = context;
            _logger = logger;
        }

        public bool IsValidIngest(PackageEntry entry)
        {
            var adiData = _context
                .Adi_Data
                .FirstOrDefault(i => i.TitlPaid == entry.TitlPaIdValue);

            if (adiData is null)
            {
                return true;
            }
            
            var isVersionUpdate = _versionChecker.IsHigherVersion(entry, adiData.VersionMajor, adiData.VersionMinor);
            return isVersionUpdate;
        }

        public bool IsValidUpdate(PackageEntry entry)
        {
            var dbEntry = _context
                .Adi_Data
                .FirstOrDefault(i => i.TitlPaid == entry.TitlPaIdValue);

            if (dbEntry?.VersionMajor != null)
            {
                entry.IsPackageAnUpdate = _versionChecker.IsHigherVersion(entry, dbEntry.VersionMajor,
                    dbEntry.VersionMinor, entry.IsTvodPackage);
            }

            if (entry.IsPackageAnUpdate && dbEntry != null)
            {
                _logger.LogInformation("Package is confirmed as a valid Update Package");
                _logger.LogInformation($"IngestUUID: {dbEntry.IngestUUID} Extracted from the database.");
                entry.IngestUuid = dbEntry.IngestUUID;
                return true;
            }

            return CheckUpdateVersionMajor(entry, dbEntry);
        }

        private bool CheckUpdateVersionMajor(PackageEntry entry, Adi_Data adiData)
        {
            if (entry.IsPackageAnUpdate || adiData == null || !entry.UpdateVersionFailure)
            {
                return true;
            }

            return false;

        }

        public bool IsValidForEnrichment(PackageEntry entry, Adi_Data dbAdiData)
        {
            var layerOneValidator = new EnrichmentLayer1Validator(entry, _logger);
            if (layerOneValidator.ValidateLayer()) return true;
            var layerTwoValidator = new EnrichmentLayer2Validator(entry, dbAdiData, _logger);
            return entry.GraceNoteData.MovieEpisodeProgramData.movieInfo != null
                               || layerTwoValidator.ValidateLayer();
        }
    }
}