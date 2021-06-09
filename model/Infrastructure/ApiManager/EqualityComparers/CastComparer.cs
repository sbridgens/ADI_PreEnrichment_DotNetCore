using System;
using System.Collections.Generic;
using Domain.Schema.GNProgramSchema;

namespace Infrastructure.ApiManager.EqualityComparers
{
    public class CastComparer : IEqualityComparer<GnApiProgramsSchema.castTypeMember>
    {
        public bool Equals(GnApiProgramsSchema.castTypeMember episodeMovieMember,
            GnApiProgramsSchema.castTypeMember seriesSeasonMember)
        {
            return (episodeMovieMember != null) & (episodeMovieMember?.name.first == seriesSeasonMember?.name.first) &
                   (episodeMovieMember?.name.last == seriesSeasonMember?.name.last);
        }

        public int GetHashCode(GnApiProgramsSchema.castTypeMember member)
        {
            return Convert.ToInt32(member.personId);
        }
    }
}