using System.Reflection;
using Fabron.Dispatching;
using Fabron.Schedulers;
using Fabron.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fabron.Server;

public class FabronServerBuilder
{
    public FabronServerBuilder(IHostBuilder hostBuilder)
    {
        if (hostBuilder.Properties.ContainsKey("HasFabronServerBuilder"))
        {
            throw new InvalidOperationException("FabronServerBuilder has already been added to this host.");
        }

        hostBuilder.Properties["HasFabronServerBuilder"] = "true";

        hostBuilder.ConfigureServices((ctx, services) =>
        {
            services.AddSingleton<ISystemClock, SystemClock>();
            services.AddSingleton<IFabronClient, FabronClient>();
            services.AddSingleton<IFireDispatcher, FireDispatcher>();
        });

        HostBuilder = hostBuilder;
    }

    public IHostBuilder HostBuilder { get; }

    public FabronServerBuilder ConfigureOrleans(Action<HostBuilderContext, ISiloBuilder> configure)
    {
        HostBuilder.ConfigureServices((context, services) =>
        {
            services.AddOrleans(sb =>
            {
                configure(context, sb);
            });
        });

        return this;
    }

    public FabronServerBuilder UseLocalhostClustering()
    {
        ConfigureOrleans((ctx, sb) =>
        {
            sb.UseLocalhostClustering();
        });
        return this;
    }

    public FabronServerBuilder UseInMemory()
    {
        HostBuilder.ConfigureServices((context, services) =>
        {
            services.AddScoped<IGenericTimerStore, InMemoryGenericTimerStore>();
            services.AddScoped<ICronTimerStore, InMemoryCronTimerStore>();
            services.AddScoped<IPeriodicTimerStore, InMemoryPeriodicTimerStore>();
        });

        ConfigureOrleans((context, siloBuilder) =>
        {
            siloBuilder
                .UseInMemoryReminderService();
        });

        return this;
    }

    public FabronServerBuilder AddSimpleFireRouter(Action<SimpleFireRouterOptions> configure)
    {
        HostBuilder.ConfigureServices((context, services) =>
        {
            services.AddSingleton<IFireRouter, SimpleFireRouter>();
            services.Configure(configure);
        });
        return this;
    }
}
