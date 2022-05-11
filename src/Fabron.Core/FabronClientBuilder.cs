using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Hosting;

namespace Fabron;

public class FabronClientBuilder
{
    public FabronClientBuilder(IHostBuilder hostBuilder, IEnumerable<Assembly>? commandAssemblies = null)
    {
        if (hostBuilder.Properties.ContainsKey("HasFabronClientBuilder"))
        {
            throw new InvalidOperationException("FabronClientBuilder has already been added to this host.");
        }

        hostBuilder.Properties["HasFabronClientBuilder"] = "true";

        hostBuilder.ConfigureServices((ctx, services) =>
        {
            services.TryAddSingleton<IJobManager, JobManager>();
            services.RegisterCommands(commandAssemblies);
        });

        hostBuilder.UseOrleansClient((ctx, cb) =>
        {
        });

        HostBuilder = hostBuilder;
    }

    public IHostBuilder HostBuilder { get; }

    public FabronClientBuilder ConfigureOrleansClient(Action<HostBuilderContext, IClientBuilder> configure)
    {
        HostBuilder.ConfigureServices((context, services) =>
        {
            services.AddOrleansClient(sb =>
            {
                configure(context, sb);
            });
        });

        return this;
    }

    public FabronClientBuilder UseLocalhostClustering()
    {
        ConfigureOrleansClient((ctx, cb) =>
        {
            cb.UseLocalhostClustering();
        });
        return this;
    }
}
