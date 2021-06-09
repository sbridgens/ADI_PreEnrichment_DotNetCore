using System;
using Application.BusinessLogic.Contracts;
using Application.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Strategies
{
    public class StrategyFactory : IStrategyFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public StrategyFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IProcessingStrategy Get(PackageEntry entry)
        {
            if (entry.IsPackageAnUpdate)
            {
                return _serviceProvider.GetRequiredService<UpdateStrategy>();
            }

            return _serviceProvider.GetRequiredService<IngestStrategy>();
        }
    }
}