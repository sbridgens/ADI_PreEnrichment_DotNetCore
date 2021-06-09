using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.DataAccess.Persistence.Contracts
{
    public interface IApplicationDbContext
    {
        DbSet<Adi_Data> Adi_Data { get; set; }
        DbSet<GN_ImageLookup> GN_ImageLookup { get; set; }
        DbSet<GN_Mapping_Data> GN_Mapping_Data { get; set; }
        DbSet<CategoryMapping> CategoryMapping { get; set; }
        DbSet<GnProgramTypeLookup> GnProgramTypeLookup { get; set; }
        DbSet<MappingsUpdateTracking> MappingsUpdateTracking { get; set; }
        DbSet<Layer1UpdateTracking> Layer1UpdateTracking { get; set; }
        DbSet<Layer2UpdateTracking> Layer2UpdateTracking { get; set; }
        DbSet<LatestUpdateIds> LatestUpdateIds { get; set; }
        DbSet<GN_Api_Lookup> GN_Api_Lookup { get; set; }
        int SaveChanges();
    }
}