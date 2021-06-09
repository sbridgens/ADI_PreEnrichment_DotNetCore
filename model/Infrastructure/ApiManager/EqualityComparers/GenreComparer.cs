using System;
using System.Collections.Generic;
using Domain.Schema.GNProgramSchema;

namespace Infrastructure.ApiManager.EqualityComparers
{
    public class GenreComparer : IEqualityComparer<GnApiProgramsSchema.programsProgramGenre>
    {
        public bool Equals(GnApiProgramsSchema.programsProgramGenre episodeGenres,
            GnApiProgramsSchema.programsProgramGenre seriesGenres)
        {
            return episodeGenres?.Value == seriesGenres?.Value;
        }

        public int GetHashCode(GnApiProgramsSchema.programsProgramGenre genres)
        {
            return Convert.ToInt32(genres.genreId);
        }
    }
}