using System.Collections.Concurrent;
using Fabron.Dispatching;
using Fabron.Schedulers;
using Fabron.Stores;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;
using Orleans.TestingHost;

namespace Fabron.FunctionalTests;

public class TestSiloConfigurator : ISiloConfigurator
{
    public virtual void ConfigureServices(IServiceCollection services)
    { }
        //=> services.AddHttpClient();

    public virtual void ConfigureSilo(ISiloBuilder siloBuilder)
    {
    }

    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder.Configure<MessagingOptions>(options =>
        {
            options.ResponseTimeout = TimeSpan.FromSeconds(5);
        });

        siloBuilder.UseInMemoryReminderService();
        siloBuilder.ConfigureServices(services =>
        {
            services.AddSingleton<IGenericTimerStore, InMemoryGenericTimerStore>();
            services.AddSingleton<ICronTimerStore, InMemoryCronTimerStore>();
            services.AddSingleton<IPeriodicTimerStore, InMemoryPeriodicTimerStore>();
            services.AddSingleton<IFireDispatcher, TestFireDispatcher>();
            services.AddSingleton<ISystemClock, SystemClock>();
        });
        siloBuilder.ConfigureServices(ConfigureServices);
    }
}

public class TestFireDispatcher : IFireDispatcher
{
    public ConcurrentBag<FireEnvelop> Fires { get; } = [];
    public Task DispatchAsync(FireEnvelop envelop)
    {
        Fires.Add(envelop);
        return Task.CompletedTask;
    }
}
