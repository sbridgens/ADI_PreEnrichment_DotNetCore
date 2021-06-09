using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Persistence.Mappings
{
    public class Layer2UpdateTrackingMap : IEntityTypeConfiguration<Layer2UpdateTracking>
    {
        public void Configure(EntityTypeBuilder<Layer2UpdateTracking> builder)
        {
            builder.ToTable(@"Layer2UpdateTracking", @"adi");
            builder.HasKey(x => x.Id);
            builder.HasOne(x => x.GnMappingData)
                .WithMany()
                .HasForeignKey(x => x.IngestUUID);
            builder.Property(x => x.GN_Paid).HasColumnName("GN_Paid");
            builder.Property(x => x.GN_connectorId).HasColumnName("GN_connectorId");
            builder.Property(x => x.Layer2_UpdateId).HasColumnName("Layer2_UpdateId");
            builder.Property(x => x.Layer2_UpdateDate).HasColumnName("Layer2_UpdateDate");
            builder.Property(x => x.Layer2_NextUpdateId).HasColumnName("Layer2_NextUpdateId");
            builder.Property(x => x.Layer2_MaxUpdateId).HasColumnName("Layer2_MaxUpdateId");
            builder.Property(x => x.Layer2_RootId).HasColumnName("Layer2_RootId");
            builder.Property(x => x.UpdatesChecked).HasColumnName("UpdatesChecked");
            builder.Property(x => x.RequiresEnrichment).HasColumnName("RequiresEnrichment");
        }
    }
}