
using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;

namespace Microsoft.Extensions.Hosting
{
    public static class FabronHostBuilderExtensions
    {
        public static IHostBuilder UseFabron(
            this IHostBuilder hostBuilder,
            Action<HostBuilderContext, ISiloBuilder> configureDelegate)
        => hostBuilder.UseFabron(configureDelegate, null);

        public static IHostBuilder UseFabron(
            this IHostBuilder hostBuilder,
            Action<HostBuilderContext, ISiloBuilder> configureDelegate,
            IEnumerable<Assembly>? assemblies)
        {
            hostBuilder.ConfigureServices((ctx, services) =>
            {
            });

            hostBuilder.UseOrleans((ctx, siloBuilder) =>
            {
                siloBuilder.AddFabron(assemblies);
                configureDelegate(ctx, siloBuilder);
            });

            return hostBuilder;
        }
    }
}
