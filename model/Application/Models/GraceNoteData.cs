using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Entities;
using MappingSchema = Domain.Schema.GNMappingSchema.GnOnApiProgramMappingSchema;
using ProgramSchema = Domain.Schema.GNProgramSchema.GnApiProgramsSchema;

namespace Application.Models
{
    public class GraceNoteData
    {
        public GN_Mapping_Data Entity { get; set; }
        public MappingSchema.@on MappingData { get; set; }
        public ProgramSchema.@on CoreProgramData { get; set; }
        public ProgramSchema.programsProgram MovieEpisodeProgramData { get; set; }
        public ProgramSchema.programsProgram ShowSeriesSeasonProgramData { get; set; }
        public MappingSchema.onProgramMappingsProgramMapping GraceNoteMappingData { get; set; }
        public EnrichmentDataLists EnrichmentDataLists { get; set; }
        public string GraceNoteTmsId { get; set; }
        public string GraceNoteRootId { get; set; }
        public string GnMappingPaid { get; set; }
        public string GraceNoteConnectorId { get; set; }
        public string GraceNoteUpdateId { get; set; }
        public ProgramSchema.@on CoreSeriesData { get; set; }
        public List<ProgramSchema.programsProgramSeason> SeasonInfo { get; set; }

        public List<ProgramSchema.externalLinksTypeExternalLink> ExternalLinks()
        {
            var externalLinks = new List<ProgramSchema.externalLinksTypeExternalLink>();

            if (MovieEpisodeProgramData.externalLinks.Any())
            {
                externalLinks.AddRange(MovieEpisodeProgramData.externalLinks);
            }

            if (ShowSeriesSeasonProgramData != null && ShowSeriesSeasonProgramData.externalLinks.Any())
            {
                externalLinks.AddRange(ShowSeriesSeasonProgramData.externalLinks);
            }

            return externalLinks;
        }

        public bool HasMovieInfo()
        {
            return MovieEpisodeProgramData?.movieInfo != null;
        }

        public string GetShowName()
        {
            return ShowSeriesSeasonProgramData?.titles.title
                       .Where(t => (t.type == "full") & (t.size == "120"))
                       .Select(t => t.Value)
                       .FirstOrDefault()
                       ?.ToString() ??
                   MovieEpisodeProgramData.titles.title
                       .Where(t => (t.type == "full") & (t.size == "120"))
                       .Select(t => t.Value)
                       .FirstOrDefault();
        }

        public int GetNumberOfSeasons()
        {
            return ShowSeriesSeasonProgramData != null
                ? Convert.ToInt32(ShowSeriesSeasonProgramData.seasons?.Count)
                : Convert.ToInt32(MovieEpisodeProgramData != null
                    ? MovieEpisodeProgramData.seasons?.Count
                    : 0);
        }

        public List<ProgramSchema.programsProgramSeason> GetSeasonInfo()
        {
            return ShowSeriesSeasonProgramData?.seasons ?? MovieEpisodeProgramData.seasons;
        }

        public DateTime GetSeriesPremiere()
        {
            return ShowSeriesSeasonProgramData?.seriesPremiere ?? MovieEpisodeProgramData.seriesPremiere;
        }

        public DateTime GetSeriesFinale()
        {
            return ShowSeriesSeasonProgramData?.seriesFinale ?? MovieEpisodeProgramData.seriesFinale;
        }

        public DateTime? GetSeasonPremiere()
        {
            var first =
                ShowSeriesSeasonProgramData?.seasons?.FirstOrDefault() ??
                MovieEpisodeProgramData.seasons?.FirstOrDefault();

            return first?.seasonPremiere;
        }

        public DateTime? GetSeasonFinale()
        {
            var last =
                ShowSeriesSeasonProgramData?.seasons?.LastOrDefault() ??
                MovieEpisodeProgramData.seasons.LastOrDefault();
            return last?.seasonFinale;
        }

        public string GetShowId(string prefixShowId = null)
        {
            return !string.IsNullOrWhiteSpace(prefixShowId)
                ? $"{prefixShowId}{ShowSeriesSeasonProgramData?.seriesId}"
                : ShowSeriesSeasonProgramData?.seriesId ??
                  MovieEpisodeProgramData.seriesId;
        }
    }
}