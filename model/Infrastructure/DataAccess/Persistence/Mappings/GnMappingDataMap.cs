using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Persistence.Mappings
{
    public class GnMappingDataMap : IEntityTypeConfiguration<GN_Mapping_Data>
    {
        public void Configure(EntityTypeBuilder<GN_Mapping_Data> builder)
        {
            builder.ToTable(@"GN_Mapping_Data", @"adi");
            builder.HasKey(x => x.IngestUUID);
            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.GN_Availability_End).HasColumnName("GN_Availability_End");
            builder.Property(x => x.GN_Availability_Start).HasColumnName("GN_Availability_Start");
            builder.Property(x => x.GN_connectorId).HasColumnName("GN_connectorId");
            builder.Property(x => x.GN_creationDate).HasColumnName("GN_creationDate");
            builder.Property(x => x.GN_EpisodeNumber).HasColumnName("GN_EpisodeNumber");
            builder.Property(x => x.GN_EpisodeTitle).HasColumnName("GN_EpisodeTitle");
            builder.Property(x => x.GN_Images).HasColumnName("GN_Images");
            builder.Property(x => x.GN_Paid).HasColumnName("GN_Paid");
            builder.Property(x => x.GN_Pid).HasColumnName("GN_Pid");
            builder.Property(x => x.GN_programMappingId).HasColumnName("GN_programMappingId");
            builder.Property(x => x.GN_ProviderId).HasColumnName("GN_ProviderId");
            builder.Property(x => x.GN_RootID).HasColumnName("GN_RootID");
            builder.Property(x => x.GN_SeasonId).HasColumnName("GN_SeasonId");
            builder.Property(x => x.GN_SeasonNumber).HasColumnName("GN_SeasonNumber");
            builder.Property(x => x.GN_SeriesId).HasColumnName("GN_SeriesId");
            builder.Property(x => x.GN_SeriesTitle).HasColumnName("GN_SeriesTitle");
            builder.Property(x => x.GN_Status).HasColumnName("GN_Status");
            builder.Property(x => x.GN_TMSID).HasColumnName("GN_TMSID");
            builder.Property(x => x.GN_updateId).HasColumnName("GN_updateId");
        }
    }
}