// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

using Orleans.Hosting;

namespace Microsoft.Extensions.Hosting
{
    public static class FabronHostBuilderExtensions
    {
        public static IHostBuilder UseFabron(this IHostBuilder hostBuilder, Assembly commandAssembly, Action<ISiloBuilder> configureDelegate)
        {
            if (configureDelegate == null)
            {
                throw new ArgumentNullException(nameof(configureDelegate));
            }

            hostBuilder.UseOrleans((ctx, siloBuilder) =>
            {
                siloBuilder.AddFabron(commandAssembly);
                configureDelegate(siloBuilder);
            });

            return hostBuilder;
        }
    }
}
