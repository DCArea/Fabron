using System;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Hosting;
using Fabron;
using Fabron.Grains.TransientJob;
using Fabron.Services;

namespace Microsoft.Extensions.Hosting
{

    public static class FabronHostBuilderExtensions
    {
        public static IHostBuilder UseFabron(this IHostBuilder builder, Action<ISiloBuilder> siloConfigure)
        {
            builder
                .ConfigureServices((ctx, services) =>
                    {
                        services.AddScoped<IMediator, Mediator>();
                        services.AddSingleton<IJobManager, JobManager>();
                    })
                .UseOrleans(siloBuilder =>
                {
                    siloBuilder
                        .ConfigureApplicationParts(manager =>
                            manager.AddApplicationPart(typeof(TransientJobGrain).Assembly).WithReferences());
                    siloConfigure(siloBuilder);
                });
            return builder;
        }
    }
}
