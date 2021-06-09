using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Persistence.Mappings
{
    public class LatestUpdateIdsMap : IEntityTypeConfiguration<LatestUpdateIds>
    {
        public void Configure(EntityTypeBuilder<LatestUpdateIds> builder)
        {
            builder.ToTable(@"LatestUpdateIds", @"adi");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.LastMappingUpdateIdChecked).HasColumnName("LastMappingUpdateIdChecked");
            builder.Property(x => x.LastLayer1UpdateIdChecked).HasColumnName("LastLayer1UpdateIdChecked");
            builder.Property(x => x.LastLayer2UpdateIdChecked).HasColumnName("LastLayer2UpdateIdChecked");
            builder.Property(x => x.InOperation).HasColumnName("InOperation");
        }
    }
}