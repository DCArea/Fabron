
using System;
using Microsoft.Extensions.DependencyInjection;

using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.TestingHost;

namespace Fabron.FunctionalTests
{
    public class TestSiloConfiguratorWithBlockedEventListeners : TestSiloConfigurator
    {
        public override void ConfigureSilo(ISiloBuilder silo) => silo.SetEventListener<BlockedEventListener, BlockedEventListener>();
    }
    public class TestSiloConfigurator : ISiloConfigurator
    {
        public virtual void ConfigureServices(Microsoft.Extensions.Hosting.HostBuilderContext context, IServiceCollection services) => services.AddHttpClient();

        public virtual void ConfigureSilo(ISiloBuilder siloBuilder)
        {
        }

        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.Configure<MessagingOptions>(options =>
            {
                options.ResponseTimeout = TimeSpan.FromSeconds(5);
            });

            siloBuilder.Configure<CronJobOptions>(options =>{
                options.UseAsynchronousIndexer = false;
            });
            siloBuilder.Configure<JobOptions>(options =>{
                options.UseAsynchronousIndexer = false;
            });
            siloBuilder.UseInMemory();

            siloBuilder.ConfigureServices(ConfigureServices);
            siloBuilder.AddFabron();
        }
    }
}
