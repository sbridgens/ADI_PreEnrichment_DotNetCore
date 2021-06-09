using System.Collections.Generic;
using System.Linq;
using Application.Models;
using Infrastructure.ApiManager.EqualityComparers;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Validation
{
    public abstract class EnrichmentValidator
    {
        protected readonly EnrichmentDataLists EnrichmentDataLists;
        private readonly ILogger _logger;
        private readonly PackageEntry _packageEntry;

        protected EnrichmentValidator(PackageEntry packageEntry, ILogger logger)
        {
            _logger = logger;
            EnrichmentDataLists = packageEntry.GraceNoteData.EnrichmentDataLists;
            _packageEntry = packageEntry;
        }
        
        /// <summary>
        /// Sets RequiresEnrichment value to true. The message is the reason of the change
        /// </summary>
        protected bool AssetRequiresEnrichment(string message)
        {
            _logger.LogDebug(message);
            return true;
        }

        /// <summary>
        /// Sets RequiresEnrichment value to true.
        /// The reason of the change is inequality of previous and api values for key parameter
        /// </summary>
        protected bool AssetRequiresEnrichment(string key, string previousValue, string apiValue, string separator = ", ")
        {
            var message = $"{key} Data changed: Previous = {previousValue}{separator}Api Value = {apiValue}";
            return AssetRequiresEnrichment(message);
        }

        protected List<string> ReturnAppDataValueList(string appDataName)
        {
            return _packageEntry.AdiData.UpdateAdi.Asset.Metadata.App_Data.Where(a => a.Name == appDataName)
                .Select(v => v.Value)
                .ToList();
        }
        
        protected string ReturnSingleAppDataValue(string appDataName)
        {
            return _packageEntry.AdiData.UpdateAdi.Asset.Metadata.App_Data
                .FirstOrDefault(a => a.Name == appDataName)
                ?.Value;
        }
        
        protected bool CheckSingleValue(string valueKey, string apiValue)
        {
            var previousValue = ReturnSingleAppDataValue(valueKey);
            if (previousValue != apiValue)
            {
                return AssetRequiresEnrichment(valueKey, previousValue, apiValue);
            }

            return false;
        }
        
        protected bool ValidateProgramGenreData(int layer)
        {
            var genres = layer == 1 ? ReturnAppDataValueList("Genre") : ReturnAppDataValueList("Show_Genre");
            var genreIds = layer == 1 ? ReturnAppDataValueList("GenreID") : ReturnAppDataValueList("Show_GenreID");

            var apiGenres = EnrichmentDataLists.GenresList?.Distinct(new GenreComparer())
                .ToList()
                .Select(g => g.Value)
                .ToList();

            var apiGenreIds = EnrichmentDataLists.GenresList?.Distinct(new GenreComparer())
                .ToList()
                .Select(g => g.genreId)
                .ToList();

            foreach (var genre in genres.Where(genre => apiGenres != null && !apiGenres.Contains(genre)))
            {
                return AssetRequiresEnrichment($"New Genre Added: {genre}");
            }

            foreach (var genreId in genreIds.Where(genreId => apiGenreIds != null && !apiGenreIds.Contains(genreId)))
            {
                return AssetRequiresEnrichment($"New Genre ID Added: {genreId}");
            }

            return false;
        }
    }
}