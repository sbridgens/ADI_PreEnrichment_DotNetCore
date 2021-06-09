using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Persistence.Mappings
{
    public class GnImageLookupMap : IEntityTypeConfiguration<GN_ImageLookup>
    {
        public void Configure(EntityTypeBuilder<GN_ImageLookup> builder)
        {
            builder.ToTable(@"GN_ImageLookup", @"adi");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Image_Lookup).HasColumnName("Image_Lookup");
            builder.Property(x => x.Image_Mapping).HasColumnName("Image_Mapping");
            builder.Property(x => x.Image_AdiOrder).HasColumnName("Image_AdiOrder");
            builder.Property(x => x.Mapping_Config).HasColumnName("Mapping_Config");
        }
    }
}