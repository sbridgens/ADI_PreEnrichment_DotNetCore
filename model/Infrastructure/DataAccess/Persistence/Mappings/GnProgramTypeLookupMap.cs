using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Persistence.Mappings
{
    public class GnProgramTypeLookupMap : IEntityTypeConfiguration<GnProgramTypeLookup>
    {
        public void Configure(EntityTypeBuilder<GnProgramTypeLookup> builder)
        {
            builder.ToTable(@"GnProgramTypeLookup", @"adi");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.GnProgramType).HasColumnName("GnProgramType");
            builder.Property(x => x.GnProgramSubType).HasColumnName("GnProgramSubType");
            builder.Property(x => x.LgiProgramType).HasColumnName("LgiProgramType");
            builder.Property(x => x.LgiProgramTypeId).HasColumnName("LgiProgramTypeId");
        }
    }
}