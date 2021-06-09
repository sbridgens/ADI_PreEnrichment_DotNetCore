using Application.BusinessLogic;
using Application.BusinessLogic.Contracts;
using Application.Configuration;
using Application.DataAccess.Persistence.Contracts;
using Application.FileManager.Contracts;
using Application.FileManager.Serialization;
using Application.Validation.Contracts;
using Infrastructure.ApiManager;
using Infrastructure.BusinessManager;
using Infrastructure.DataAccess.Persistence;
using Infrastructure.DataAccess.Persistence.Contexts;
using Infrastructure.FileManager;
using Infrastructure.FileManager.ZipArchive;
using Infrastructure.ImageLogic;
using Infrastructure.Strategies;
using Infrastructure.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
                options.UseNpgsql(
                    serviceProvider.GetRequiredService<IOptions<DatabaseSettings>>().Value.ConnectionString,
                    builder => builder.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
            services.AddScoped<IApplicationDbContext>(provider => provider.GetService<ApplicationDbContext>());
            services.AddScoped<IQueuedProcessor, QueuedProcessor>();
            services.AddScoped<IImageWorker, ImageWorker>();
            services.AddScoped<IGnMappingDataStore, MappingDataStore>();
            services.AddScoped<IVersionChecker, VersionChecker>();
            services.AddScoped<IPackageValidator, PackageValidator>();
            services.AddScoped<IngestStrategy>();
            services.AddScoped<UpdateStrategy>();
            services.AddScoped<IGraceNoteMetadataProvider, GraceNoteMetadataProvider>();
            services.AddScoped<IGraceNoteApi, GraceNoteApi>();
            services.AddScoped<IProgramTypeLookupStore, ProgramTypeLookupStore>();
            services.AddScoped<IMetadataMapper, MetadataMapper>();
            services.AddScoped<IStrategyFactory, StrategyFactory>();
            services.AddScoped<IPackageProcessor, AdiPackageProcessor>();
            services.AddScoped<IMetadataUpdater, MetadataUpdater>();
 
            services.AddSingleton<IFileSystemWorker, FileSystemWorker>();
            services.AddSingleton<IZipArchiveWorker, ZipArchiveWorker>();
            services.AddSingleton<IAdiImporter, AdiImporter>();
            services.AddSingleton<IAdiValidator, AdiValidator>();
            services.AddSingleton<IPackageImporter, PackageImporter>();
            services.AddSingleton<ISystemClock, SystemClock>();
            services.AddSingleton<IXmlSerializationManager, XmlSerializationManager>();
            
            return services;
        }
        
        public static IServiceCollection AddTrackingInfrastructure(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
                options.UseNpgsql(
                    serviceProvider.GetRequiredService<IOptions<DatabaseSettings>>().Value.ConnectionString,
                    builder => builder.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
            services.AddScoped<IApplicationDbContext>(provider => provider.GetService<ApplicationDbContext>());
            services.AddScoped<IQueuedProcessor, QueuedProcessor>();
            services.AddScoped<IImageWorker, ImageWorker>();
            services.AddScoped<IGnMappingDataStore, MappingDataStore>();
            services.AddScoped<IVersionChecker, VersionChecker>();
            services.AddScoped<IPackageValidator, PackageValidator>();
            services.AddScoped<IngestStrategy>();
            services.AddScoped<UpdateStrategy>();
            services.AddScoped<IGraceNoteMetadataProvider, GraceNoteMetadataProvider>();
            services.AddScoped<IGraceNoteApi, GraceNoteApi>();
            services.AddScoped<IProgramTypeLookupStore, ProgramTypeLookupStore>();
            services.AddScoped<IMetadataMapper, MetadataMapper>();
            services.AddScoped<IStrategyFactory, StrategyFactory>();
            services.AddScoped<IPackageProcessor, AdiPackageProcessor>();
            services.AddScoped<IMetadataUpdater, MetadataUpdater>();
            services.AddScoped<IMappingsUpdateTrackingStore, MappingsUpdateTrackingStore>();
            services.AddScoped<ILayer1UpdateTrackingStore, Layer1UpdateTrackingStore>();
            services.AddScoped<ILayer2UpdateTrackingStore, Layer2UpdateTrackingStore>();
            services.AddScoped<IGnTrackingOperations, TrackingProcessor>();
            services.AddScoped<IDatabaseExistenceChecker, DatabaseExistenceChecker>();
            services.AddScoped<IGnUpdatesProcessor, UpdatesProcessor>();
 
            services.AddSingleton<IFileSystemWorker, FileSystemWorker>();
            services.AddSingleton<IZipArchiveWorker, ZipArchiveWorker>();
            services.AddSingleton<IAdiImporter, AdiImporter>();
            services.AddSingleton<IAdiValidator, AdiValidator>();
            services.AddSingleton<IPackageImporter, PackageImporter>();
            services.AddSingleton<ISystemClock, SystemClock>();
            services.AddSingleton<IXmlSerializationManager, XmlSerializationManager>();
            
            return services;
        }
        
        
        public static IServiceCollection AddGeneratorInfrastructure(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
                options.UseNpgsql(
                    serviceProvider.GetRequiredService<IOptions<DatabaseSettings>>().Value.ConnectionString,
                    builder => builder.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
            services.AddScoped<IApplicationDbContext>(provider => provider.GetService<ApplicationDbContext>());
            services.AddScoped<IQueuedProcessor, QueuedProcessor>();
            services.AddScoped<IImageWorker, ImageWorker>();
            services.AddScoped<IGnMappingDataStore, MappingDataStore>();
            services.AddScoped<IVersionChecker, VersionChecker>();
            services.AddScoped<IPackageValidator, PackageValidator>();
            services.AddScoped<IngestStrategy>();
            services.AddScoped<UpdateStrategy>();
            services.AddScoped<IGraceNoteMetadataProvider, GraceNoteMetadataProvider>();
            services.AddScoped<IGraceNoteApi, GraceNoteApi>();
            services.AddScoped<IProgramTypeLookupStore, ProgramTypeLookupStore>();
            services.AddScoped<IMetadataMapper, MetadataMapper>();
            services.AddScoped<IStrategyFactory, StrategyFactory>();
            services.AddScoped<IPackageProcessor, AdiPackageProcessor>();
            services.AddScoped<IMetadataUpdater, MetadataUpdater>();
            services.AddScoped<IMappingsUpdateTrackingStore, MappingsUpdateTrackingStore>();
            services.AddScoped<ILayer1UpdateTrackingStore, Layer1UpdateTrackingStore>();
            services.AddScoped<ILayer2UpdateTrackingStore, Layer2UpdateTrackingStore>();
            services.AddScoped<IGnTrackingOperations, GeneratorProcessor>();
            services.AddScoped<IDatabaseExistenceChecker, DatabaseExistenceChecker>();
            services.AddScoped<IGnUpdatesProcessor, UpdatesProcessor>();
            services.AddScoped<IDataFetcher, DataFetcher>();
            services.AddScoped<IGnMappingComparer, MappingComparer>();
            services.AddScoped<IGnLayerDataComprarer, GnLayerDataComparer>();
            services.AddScoped<IWorkflowExecutor, WorkflowExecutor>();
 
            services.AddSingleton<IFileSystemWorker, FileSystemWorker>();
            services.AddSingleton<IZipArchiveWorker, ZipArchiveWorker>();
            services.AddSingleton<IAdiImporter, AdiImporter>();
            services.AddSingleton<IAdiValidator, AdiValidator>();
            services.AddSingleton<IPackageImporter, PackageImporter>();
            services.AddSingleton<ISystemClock, SystemClock>();
            services.AddSingleton<IXmlSerializationManager, XmlSerializationManager>();
            
            return services;
        }
    }
}