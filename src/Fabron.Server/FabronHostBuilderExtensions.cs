
using System;
using System.Collections.Generic;
using System.Reflection;
using Fabron;
using Fabron.Events;
using Fabron.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Hosting;

namespace Microsoft.Extensions.Hosting
{
    public static class FabronHostBuilderExtensions
    {
        public static IHostBuilder UseFabron(this IHostBuilder hostBuilder, Action<HostBuilderContext, ISiloBuilder> configureDelegate)
            => hostBuilder.UseFabron(configureDelegate, null);

        public static IHostBuilder UseFabron(this IHostBuilder hostBuilder, Action<HostBuilderContext, ISiloBuilder> configureDelegate, IEnumerable<Assembly>? assemblies)
        {
            if (configureDelegate == null)
            {
                throw new ArgumentNullException(nameof(configureDelegate));
            }

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
