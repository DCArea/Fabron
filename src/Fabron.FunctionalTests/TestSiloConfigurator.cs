
using System;
using Fabron.Core.CloudEvents;
using Fabron.Store;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.TestingHost;

namespace Fabron.FunctionalTests
{
    public class TestSiloConfigurator : ISiloConfigurator
    {
        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient();
        }

        public virtual void ConfigureSilo(ISiloBuilder siloBuilder)
        {
        }

        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.Configure<MessagingOptions>(options =>
            {
                options.ResponseTimeout = TimeSpan.FromSeconds(5);
            });

            siloBuilder.UseLocalhostClustering()
                .UseInMemoryReminderService();
            siloBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<ITimedEventStore, InMemoryTimedEventStore>();
                services.AddSingleton<ICronEventStore, InMemoryCronEventStore>();
                services.AddSingleton<IEventDispatcher, EventDispatcher>();
                services.AddSingleton<ISystemClock, SystemClock>();
            });
            siloBuilder.ConfigureServices(ConfigureServices);
        }
    }
}
