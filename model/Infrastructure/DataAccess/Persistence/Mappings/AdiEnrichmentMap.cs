using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Persistence.Mappings
{
    public class AdiEnrichmentMap : IEntityTypeConfiguration<Adi_Data>
    {
        public void Configure(EntityTypeBuilder<Adi_Data> builder)
        {
            builder.ToTable(@"Adi_Data", @"adi");
            builder.HasKey(x => x.Id);
            builder.HasOne(x => x.GnMappingData)
                .WithMany()
                .HasForeignKey(x => x.IngestUUID);
            
            builder.Property(x => x.TitlPaid).HasColumnName("TITLPAID");
            builder.Property(x => x.OriginalAdi).HasColumnName("OriginalADI");
            builder.Property(x => x.VersionMajor).HasColumnName("VersionMajor");
            builder.Property(x => x.VersionMinor).HasColumnName("VersionMinor");
            builder.Property(x => x.ProviderId).HasColumnName("ProviderId");
            builder.Property(x => x.TmsId).HasColumnName("TMSID");
            builder.Property(x => x.ProcessedDateTime).HasColumnName("ProcessedDateTime");
            builder.Property(x => x.ContentTsFile).HasColumnName("ContentTSFile");
            builder.Property(x => x.ContentTsFilePaid).HasColumnName("ContentTsFilePaid");
            builder.Property(x => x.ContentTsFileChecksum).HasColumnName("ContentTSFileChecksum");
            builder.Property(x => x.ContentTsFileSize).HasColumnName("ContentTSFileSize");
            builder.Property(x => x.PreviewFile).HasColumnName("PreviewFile");
            builder.Property(x => x.PreviewFilePaid).HasColumnName("PreviewFilePaid");
            builder.Property(x => x.PreviewFileChecksum).HasColumnName("PreviewFileChecksum");
            builder.Property(x => x.PreviewFileSize).HasColumnName("PreviewFileSize");
            builder.Property(x => x.EnrichedAdi).HasColumnName("EnrichedADI");
            builder.Property(x => x.Enrichment_DateTime).HasColumnName("Enrichment_DateTime");
            builder.Property(x => x.UpdateAdi).HasColumnName("UpdateAdi");
            builder.Property(x => x.Update_DateTime).HasColumnName("Update_DateTime");
            builder.Property(x => x.Licensing_Window_End).HasColumnName("Licensing_Window_End");
        }
    }
}