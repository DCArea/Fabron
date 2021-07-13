// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

using Fabron.Grains.TransientJob;

using Microsoft.Extensions.DependencyInjection;

namespace Orleans.Hosting
{
    public static class FabronSiloBuilderExtensions
    {
        public static ISiloBuilder AddFabron(this ISiloBuilder siloBuilder, Assembly commandAssembly)
        {
            siloBuilder
                .ConfigureServices((ctx, services) =>
                {
                    services.AddFabronCore();
                });
            siloBuilder
                .ConfigureApplicationParts(manager =>
                    manager.AddApplicationPart(typeof(TransientJobGrain).Assembly).WithReferences());

            siloBuilder.ConfigureServices((ctx, services) =>
            {
                services.RegisterJobCommandHandlers(commandAssembly);
            });

            return siloBuilder;
        }

        public static ISiloBuilder UseInMemoryJobStore(this ISiloBuilder siloBuilder)
        {
            siloBuilder
                .UseInMemoryReminderService()
                .AddMemoryGrainStorage("JobStore");
            return siloBuilder;
        }
    }
}
