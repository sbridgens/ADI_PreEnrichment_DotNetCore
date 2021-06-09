using System;
using System.Linq;

namespace Domain.Schema.GNProgramSchema.Extensions
{
    public static class ProgramsProgramExtensions
    {
        
        public static string GetConnectorId(this GnApiProgramsSchema.programsProgram program)
        {
            //"SH025371110000"
            if (string.IsNullOrEmpty(program.connectorId))
                throw new Exception("No Gracenote ConnectorId for package at this time, failing ingest.");
            return program.connectorId;
        }


        public static string GetEpisodeTitle(this GnApiProgramsSchema.programsProgram program)
        {
            if (program.movieInfo != null)
                return null;

            return program.episodeInfo?
                       .title != null
                ? program.episodeInfo?.title.Value
                : program.episodeInfo?.number != null
                    ? $"Episode {program.episodeInfo?.number}"
                    : $"{program.GetSeriesTitle() ?? program.partNumber}";
        }
        
        public static string GetEpisodeSeason(this GnApiProgramsSchema.programsProgram program)
        {
            return program.episodeInfo?.season;
        }

        public static string GetSeriesTitle(this GnApiProgramsSchema.programsProgram program)
        {
            if (program.movieInfo != null)
                return null;

            return program
                .titles
                .title
                .Where(t => t.subType.Contains("Main")
                    ? t.subType.Equals("Main")
                    : t.subType.Equals("AKA"))
                .Select(r => r.Value)
                .FirstOrDefault();
        }

        public static string GetSeriesId(this GnApiProgramsSchema.programsProgram program)
        {
            return Convert.ToInt32(program.seriesId) > 0
                ? program.seriesId
                : program.GetSeasonId() > 0
                    ? program.GetSeasonId().ToString()
                    : program.GetConnectorId();
        }

        public static int GetSeasonId(this GnApiProgramsSchema.programsProgram program)
        {
            var sId = Convert.ToInt32(program?.seasonId);
            return sId > 0 ? sId : 0;
        }

        public static string GetEpisodeOrdinalValue(this GnApiProgramsSchema.programsProgram program)
        {
            var num = Convert.ToInt32(program.episodeInfo?.number);

            return Convert.ToInt32(num) > 0
                ? num.ToString()
                : (num == 0
                    ? "100001"
                    : num.ToString());
        }

        public static string GetSeriesOrdinalValue(this GnApiProgramsSchema.programsProgram program)
        {
            return Convert.ToInt32(program.seasonId) == 0
                ? "100000"
                : program.episodeInfo?.season;
        }

        public static string GetGnSeriesId(this GnApiProgramsSchema.programsProgram program)
        {
            return program.GetSeasonId() == 0
                ? program.GetConnectorId()
                : program.GetSeasonId().ToString();
        }

    }
}