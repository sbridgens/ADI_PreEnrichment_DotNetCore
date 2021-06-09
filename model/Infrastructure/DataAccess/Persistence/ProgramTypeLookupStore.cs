using System;
using System.Linq;
using Application.DataAccess.Persistence.Contracts;
using Application.Models;
using Microsoft.Extensions.Logging;

namespace Infrastructure.DataAccess.Persistence
{
    public class ProgramTypeLookupStore : IProgramTypeLookupStore
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<ProgramTypeLookupStore> _logger;

        public ProgramTypeLookupStore(IApplicationDbContext context, ILogger<ProgramTypeLookupStore> logger)
        {
            _context = context;
            _logger = logger;
        }

        public void SetProgramType(PackageEntry entry, bool isTrackerService = false)
        {
            try
            {
                if (!isTrackerService)
                {
                    _logger.LogInformation("Setting Program Types for Package.");
                }

                var progType = entry.GraceNoteData.MovieEpisodeProgramData.progType.ToLower();
                var subType = entry.GraceNoteData.MovieEpisodeProgramData.subType.ToLower();

                var lookup = _context.GnProgramTypeLookup
                    .ToList()
                    .Where(x =>
                            x.GnProgramType.ToLowerInvariant() == progType && 
                            x.GnProgramSubType.ToLowerInvariant() == subType)
                    .Select(x => x.LgiProgramTypeId)
                    .FirstOrDefault();

                //set all 3 flags to ensure static flags are set correctly per package.
                entry.IsMoviePackage = false;
                entry.IsEpisodeSeries = false;
                entry.PackageIsAOneOffSpecial = false;

                switch (lookup)
                {
                    case 0:
                        if (!isTrackerService)
                        {
                            _logger.LogInformation("Program is of type Movie");
                        }

                        entry.IsMoviePackage = true;
                        break;
                    case 1:
                    case 2:
                        if (!isTrackerService)
                        {
                            _logger.LogInformation(lookup == 1
                                ? "Program is of type Episode"
                                : "Movie is a Series/Show asset.");
                        }

                        entry.IsEpisodeSeries = true;
                        break;
                    case 99:
                        entry.PackageIsAOneOffSpecial = true;
                        if (!isTrackerService)
                        {
                            _logger.LogInformation("Program is of type Special.");
                        }

                        break;
                }
            }
            catch (Exception gptException)
            {
                _logger.LogError($"Error Encountered Setting Program Type: {gptException.Message}");
            }
        }
    }
}