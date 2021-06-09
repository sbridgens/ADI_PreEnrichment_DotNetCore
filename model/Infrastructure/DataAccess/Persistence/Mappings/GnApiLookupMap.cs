using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Persistence.Mappings
{
    public class GnApiLookupMap : IEntityTypeConfiguration<GN_Api_Lookup>
    {
        public void Configure(EntityTypeBuilder<GN_Api_Lookup> builder)
        {
            builder.ToTable(@"GN_Api_Lookup", @"adi");
            builder.HasOne(x => x.GnMappingData)
                .WithMany()
                .HasForeignKey(x => x.IngestUUID);
            builder.Property(x => x.GN_TMSID).HasColumnName("GN_TMSID");
            builder.Property(x => x.GN_connectorId).HasColumnName("GN_connectorId");
            builder.Property(x => x.GnMapData).HasColumnName("GnMapData");
            builder.Property(x => x.GnLayer1Data).HasColumnName("GnLayer1Data");
            builder.Property(x => x.GnLayer2Data).HasColumnName("GnLayer2Data");
        }
    }
}