using Fabron.Dispatching;
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
                services.AddScoped<IGenericTimerStore, InMemoryGenericTimerStore>();
                services.AddScoped<ICronTimerStore, InMemoryCronTimerStore>();
                services.AddScoped<IPeriodicTimerStore, InMemoryPeriodicTimerStore>();
                services.AddSingleton<IFireDispatcher, FireDispatcher>();
                services.AddSingleton<ISystemClock, SystemClock>();
            });
            siloBuilder.ConfigureServices(ConfigureServices);
        }
    }
}
