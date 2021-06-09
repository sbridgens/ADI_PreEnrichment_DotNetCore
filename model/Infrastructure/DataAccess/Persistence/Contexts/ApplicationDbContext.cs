using System.Reflection;
using Application.DataAccess.Persistence.Contracts;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAccess.Persistence.Contexts
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> contextOptions) : base(contextOptions)
        {
        }

        public virtual DbSet<Adi_Data> Adi_Data { get; set; }
        public virtual DbSet<GN_ImageLookup> GN_ImageLookup { get; set; }
        public virtual DbSet<GN_Mapping_Data> GN_Mapping_Data { get; set; }
        public virtual DbSet<CategoryMapping> CategoryMapping { get; set; }
        public virtual DbSet<GnProgramTypeLookup> GnProgramTypeLookup { get; set; }
        public virtual DbSet<MappingsUpdateTracking> MappingsUpdateTracking { get; set; }
        public virtual DbSet<Layer1UpdateTracking> Layer1UpdateTracking { get; set; }
        public virtual DbSet<Layer2UpdateTracking> Layer2UpdateTracking { get; set; }
        public virtual DbSet<LatestUpdateIds> LatestUpdateIds { get; set; }
        public virtual DbSet<GN_Api_Lookup> GN_Api_Lookup { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(
                    "User ID=root;Password=password;Host=localhost;Port=5432;Database=ADI_DB;Pooling=true;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            base.OnModelCreating(modelBuilder);
        }
    }
}