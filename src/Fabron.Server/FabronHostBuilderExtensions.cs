// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;

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
                configureDelegate(ctx, siloBuilder);
            });

            return hostBuilder;
        }
    }
}
