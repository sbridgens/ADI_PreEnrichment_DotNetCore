using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Persistence.Mappings
{
    public class CategoryMappingMap : IEntityTypeConfiguration<CategoryMapping>
    {
        public void Configure(EntityTypeBuilder<CategoryMapping> builder)
        {
            builder.ToTable(@"CategoryMapping", @"adi");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ProviderId).HasColumnName("ProviderId");
            builder.Property(x => x.ProviderName).HasColumnName("ProviderName");
            builder.Property(x => x.CategoryValue).HasColumnName("CategoryValue");
        }
    }
}