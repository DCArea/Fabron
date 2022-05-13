using System;
using System.Collections.Generic;
using System.Reflection;
using Fabron.Mando;
using Fabron.Store;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;

namespace Fabron;

public class FabronServerBuilder
{
    public FabronServerBuilder(IHostBuilder hostBuilder, IEnumerable<Assembly>? commandAssemblies = null)
    {
        if (hostBuilder.Properties.ContainsKey("HasFabronServerBuilder"))
        {
            throw new InvalidOperationException("FabronServerBuilder has already been added to this host.");
        }

        hostBuilder.Properties["HasFabronServerBuilder"] = "true";

        hostBuilder.ConfigureServices((ctx, services) =>
        {
            services.AddSingleton<IJobManager, JobManager>();
            services.AddScoped<IMediator, Mediator>()
                .RegisterJobCommandHandlers(commandAssemblies);
            services.AddSingleton<ISystemClock, SystemClock>();
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
            services.AddSingleton<IJobStore, InMemoryJobStore>();
            services.AddSingleton<ICronJobStore, InMemoryCronJobStore>();
        });

        ConfigureOrleans((context, siloBuilder) =>
        {
            siloBuilder
                .UseInMemoryReminderService();
        });

        return this;
    }
}
