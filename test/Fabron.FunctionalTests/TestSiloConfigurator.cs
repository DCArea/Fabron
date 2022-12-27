using Fabron.CloudEvents;
using Fabron.Schedulers;
using Fabron.Stores;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;
using Orleans.TestingHost;

namespace Fabron.FunctionalTests
{
    public class TestSiloConfigurator : ISiloConfigurator
    {
        public virtual void ConfigureServices(IServiceCollection services) => services.AddHttpClient();

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
                services.AddScoped<ITimedEventStore, InMemoryTimedEventStore>();
                services.AddScoped<ICronEventStore, InMemoryCronEventStore>();
                services.AddScoped<IPeriodicEventStore, InMemoryPeriodicEventStore>();
                services.AddSingleton<IEventDispatcher, EventDispatcher>();
                services.AddSingleton<ISystemClock, SystemClock>();
            });
            siloBuilder.ConfigureServices(ConfigureServices);
        }
    }
}
