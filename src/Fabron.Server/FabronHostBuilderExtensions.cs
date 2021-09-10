
using System;
using System.Collections.Generic;
using System.Reflection;
using Fabron.Events;
using Fabron.Stores;
using Microsoft.Extensions.DependencyInjection;
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

            hostBuilder.UseOrleans((ctx, siloBuilder) =>
            {
                siloBuilder.AddFabron(assemblies);
                siloBuilder.ConfigureServices(services =>
                {
                    services.AddSingleton<IJobEventStore, InMemoryJobEventStore>();
                    services.AddSingleton<IJobEventListener, ConsoleJobEventListener>();
                });
                configureDelegate(ctx, siloBuilder);
            });

            return hostBuilder;
        }
    }
}
