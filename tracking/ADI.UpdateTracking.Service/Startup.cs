using System;
using Application.Configuration;
using Infrastructure;
using Infrastructure.DataAccess.Persistence.Contexts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prometheus;

namespace ADI.UpdateTracking.Service
{
    public class Startup
    {
        public IConfiguration Configuration;
        
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddDbContextCheck<ApplicationDbContext>();
            services.Configure<DatabaseSettings>(Configuration.GetSection("DatabaseSettings"));
            services.Configure<EnrichmentSettings>(Configuration.GetSection("EnrichmentSettings"));
            services.Configure<GN_UpdateTracker_Config>(Configuration.GetSection("UpdateTrackingSettings"));
            services.AddTrackingInfrastructure(Configuration);
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app,
            IWebHostEnvironment env,
            ApplicationDbContext context,
            IOptions<DatabaseSettings> databaseSettings,
            ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            EnsureDbIsCreated(context, databaseSettings, logger);

            app.UseHttpsRedirection();
            
            app.UseRouting();
            app.UseHttpMetrics();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/healthz");
                endpoints.MapControllers();
                endpoints.MapMetrics();
            });
        }
        
        private static void EnsureDbIsCreated(ApplicationDbContext context, 
            IOptions<DatabaseSettings> databaseSettings, 
            ILogger<Startup> logger)
        {
            try
            {
                logger.LogInformation($"Using database: {databaseSettings.Value.Host} : {databaseSettings.Value.Name}");
                System.Threading.Thread.Sleep(2_000); // added to eliminate concurrent migrations

                context.Database.Migrate();
                context.SeedData();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to ensure DB is created.");
            }
        }
    }
}