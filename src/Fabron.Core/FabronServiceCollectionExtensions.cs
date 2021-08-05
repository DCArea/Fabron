// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Fabron;
using Fabron.Mando;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using Nest;

namespace Microsoft.Extensions.DependencyInjection
{

    public static class FabronServiceCollectionExtensions
    {
        public static IServiceCollection AddFabronCore(this IServiceCollection services)
        {
            services.TryAddScoped<IMediator, Mediator>();
            services.TryAddSingleton<IJobManager, JobManager>();
            return services;
        }

        public static IServiceCollection AddJobReporter<TJobReporter>(this IServiceCollection services)
            where TJobReporter: class, IJobReporter 
        {
            services.AddSingleton<IJobReporter, TJobReporter>();
            return services;
        }

        public static IServiceCollection AddElasticSearchJobReporter(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<ElasticSearchJobReporterOptions>(config);
            return services.AddElasticSearchJobReporter();
        }

        public static IServiceCollection AddElasticSearchJobReporter(this IServiceCollection services, Action<ElasticSearchJobReporterOptions> configure)
        {
            services.Configure<ElasticSearchJobReporterOptions>(configure);
            return services.AddElasticSearchJobReporter();
        }

        public static IServiceCollection AddElasticSearchJobReporter(this IServiceCollection services)
        {
            services.AddSingleton<IJobReporter, ElasticSearchJobReporter>(sp =>
            {
                ElasticSearchJobReporterOptions options = sp.GetRequiredService<IOptions<ElasticSearchJobReporterOptions>>().Value;
                var settings = new ConnectionSettings(new Uri(options.Server));
                ElasticClient? client = new(settings);
                return ActivatorUtilities.CreateInstance<ElasticSearchJobReporter>(sp, client);
            });
            return services;
        }
    }
}
