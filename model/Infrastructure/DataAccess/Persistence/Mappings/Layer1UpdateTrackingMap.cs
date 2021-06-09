using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Persistence.Mappings
{
    public class Layer1UpdateTrackingMap : IEntityTypeConfiguration<Layer1UpdateTracking>
    {
        public void Configure(EntityTypeBuilder<Layer1UpdateTracking> builder)
        {
            builder.ToTable(@"Layer1UpdateTracking", @"adi");
            builder.HasKey(x => x.Id);
            builder.HasOne(x => x.GnMappingData)
                .WithMany()
                .HasForeignKey(x => x.IngestUUID);
            builder.Property(x => x.GN_Paid).HasColumnName("GN_Paid");
            builder.Property(x => x.GN_TMSID).HasColumnName("GN_TMSID");
            builder.Property(x => x.Layer1_UpdateId).HasColumnName("Layer1_UpdateId");
            builder.Property(x => x.Layer1_UpdateDate).HasColumnName("Layer1_UpdateDate");
            builder.Property(x => x.Layer1_NextUpdateId).HasColumnName("Layer1_NextUpdateId");
            builder.Property(x => x.Layer1_MaxUpdateId).HasColumnName("Layer1_MaxUpdateId");
            builder.Property(x => x.Layer1_RootId).HasColumnName("Layer1_RootId");
            builder.Property(x => x.UpdatesChecked).HasColumnName("UpdatesChecked");
            builder.Property(x => x.RequiresEnrichment).HasColumnName("RequiresEnrichment");
        }
    }
}