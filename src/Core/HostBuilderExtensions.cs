using System;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Hosting;
using TGH;
using TGH.Grains.TransientJob;
using TGH.Services;

namespace Microsoft.Extensions.Hosting
{

    public static class TGHHostBuilderExtensions
    {
        public static IHostBuilder UseTGH(this IHostBuilder builder, Action<ISiloBuilder> siloConfigure)
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
