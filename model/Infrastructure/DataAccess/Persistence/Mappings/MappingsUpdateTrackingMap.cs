using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Persistence.Mappings
{
    public class MappingsUpdateTrackingMap : IEntityTypeConfiguration<MappingsUpdateTracking>
    {
        public void Configure(EntityTypeBuilder<MappingsUpdateTracking> builder)
        {
            builder.ToTable(@"MappingsUpdateTracking", @"adi");
            builder.HasKey(x => x.Id);
            builder.HasOne(x => x.GnMappingData)
                .WithMany()
                .HasForeignKey(x => x.IngestUUID);
            builder.Property(x => x.GN_ProviderId).HasColumnName("GN_ProviderId");
            builder.Property(x => x.Mapping_UpdateId).HasColumnName("Mapping_UpdateId");
            builder.Property(x => x.Mapping_UpdateDate).HasColumnName("Mapping_UpdateDate");
            builder.Property(x => x.Mapping_NextUpdateId).HasColumnName("Mapping_NextUpdateId");
            builder.Property(x => x.Mapping_MaxUpdateId).HasColumnName("Mapping_MaxUpdateId");
            builder.Property(x => x.Mapping_RootId).HasColumnName("Mapping_RootId");
            builder.Property(x => x.UpdatesChecked).HasColumnName("UpdatesChecked");
            builder.Property(x => x.RequiresEnrichment).HasColumnName("RequiresEnrichment");
        }
    }
}