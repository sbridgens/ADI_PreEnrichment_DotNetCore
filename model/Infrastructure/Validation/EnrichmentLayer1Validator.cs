using System.Collections.Generic;
using System.Linq;
using Application.Models;
using Infrastructure.ApiManager.EqualityComparers;
using Infrastructure.BusinessManager;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Validation
{
    public class EnrichmentLayer1Validator : EnrichmentValidator
    {
        private static class Layer1Keys
        {
            public const string Descriptors = "Summary_Short";
            public const string Year = "Year";
            public const string Layer1TmsId = "GN_Layer1_TMSId";
            public const string Layer1RootId = "GN_Layer1_RootId";
            public const string ImdbId = "IMDb_ID";
            public const string ImdbShowId = "Show_IMDb_ID";

            public static readonly List<string> CrewMembers = new List<string>
            {
                "Director",
                "Producer",
                "Executive Producer",
                "Writer",
                "Screenwriter"
            };
        }

        private readonly GraceNoteData _graceNoteData;
        
        public EnrichmentLayer1Validator(PackageEntry packageEntry, ILogger logger)
        : base(packageEntry, logger)
        {
            _graceNoteData = packageEntry.GraceNoteData;
        }
        
        public bool ValidateLayer()
        {
            return ValidateActors() 
                   || ValidateCrewData() 
                   || ValidateDescriptionData() 
                   || ValidateProgramGenreData(1) 
                   || ValidateProgramTitles() 
                   || ValidateYearData() 
                   || ValidateLayer1IdData() 
                   || ValidateImdbData();
        }

        private bool ValidateActors()
        {
            var actorsList = ReturnAppDataValueList("Actors");
            var apiActors = EnrichmentDataLists.CastMembers?.Distinct(new CastComparer())
                .ToList()
                .Where(member => member.role.Equals("Actor") || member.role.Equals("Voice"))
                .Select(member => $"{member.name.first} {member.name.last}")
                .ToList();

            foreach (var actor in actorsList.Where(actor => apiActors != null && !apiActors.Contains(actor)))
            {
                return AssetRequiresEnrichment($"New Actors Data Available: {actor}");
            }

            return false;
        }
        
        private bool ValidateCrewData()
        {
            var apiCrewList = EnrichmentDataLists.CrewMembers?.Distinct(new CrewComparer())
                .ToList()
                .Select(member => $"{member.name.first} {member.name.last}")
                .ToList();
            
            var xmlCrewList = new List<string>();

            foreach (var memberData in Layer1Keys.CrewMembers
                .Select(ReturnAppDataValueList)
                .Where(memberData => memberData != null))
            {
                xmlCrewList.AddRange(memberData);
            }


            foreach (var member in xmlCrewList.Where(member => apiCrewList != null && !apiCrewList.Contains(member)))
            {
                return AssetRequiresEnrichment($"New Crew Members Added: {member}");
            }

            return false;
        }

        private bool ValidateDescriptionData()
        {
            var previousDescriptors = ReturnSingleAppDataValue(Layer1Keys.Descriptors);
            var apiDescriptors = MetadataMapper.CheckAndReturnDescriptionData(_graceNoteData.MovieEpisodeProgramData.descriptions);
            if (previousDescriptors != apiDescriptors)
            {
                return AssetRequiresEnrichment(Layer1Keys.Descriptors, previousDescriptors, apiDescriptors, "\r\n");
            }

            return false;
        }

        private bool ValidateProgramTitles()
        {
            var adiTitle = ReturnSingleAppDataValue("Title");
            if (!EnrichmentDataLists.ProgramTitles.Any(t => t.Value.Equals(adiTitle)))
            {
                return AssetRequiresEnrichment($"Title Data changed Value \"{adiTitle}\" Does not exist in API Titles List");
            }
            return false;
        }

        private bool ValidateYearData()
        {
            var previousYear = ReturnSingleAppDataValue(Layer1Keys.Year);
            var adiYearValue = _graceNoteData.MovieEpisodeProgramData.origAirDate.Year.ToString().Length == 4
                ? _graceNoteData.MovieEpisodeProgramData.origAirDate.Year.ToString()
                : _graceNoteData.MovieEpisodeProgramData.movieInfo?.yearOfRelease.Length == 4
                    ? _graceNoteData.MovieEpisodeProgramData.movieInfo.yearOfRelease
                    : string.Empty;

            if (!string.IsNullOrEmpty(adiYearValue) & previousYear != adiYearValue)
            {
                return AssetRequiresEnrichment($"{Layer1Keys.Year} Data changed: {adiYearValue}, {previousYear}");
            }

            return false;
        }

        private bool ValidateLayer1IdData()
        {
            var result = CheckSingleValue(Layer1Keys.Layer1TmsId, _graceNoteData.MovieEpisodeProgramData.TMSId);
            if (result) return true;
            return CheckSingleValue(Layer1Keys.Layer1RootId, _graceNoteData.MovieEpisodeProgramData.rootId);
        }

        private bool ValidateImdbData()
        {
            if (!_graceNoteData.MovieEpisodeProgramData.externalLinks.Any()) return false;
            
            var links = _graceNoteData.MovieEpisodeProgramData.externalLinks;
            var result = CheckSingleValue(Layer1Keys.ImdbId, links.FirstOrDefault()?.id);
            if (result) return true;

            var previousImdbId = ReturnSingleAppDataValue(Layer1Keys.ImdbShowId);
            if (string.IsNullOrEmpty(previousImdbId)) return false;
            
            var showLink = links.Last()?.id ?? links.FirstOrDefault()?.id;
            if (!string.IsNullOrEmpty(showLink) & showLink != previousImdbId)
            {
                return AssetRequiresEnrichment(Layer1Keys.ImdbShowId, previousImdbId, showLink);
            }

            return false;
        }
    }
}