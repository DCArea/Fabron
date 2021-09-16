
using System;

using Fabron;
using Fabron.ElasticSearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using Nest;

namespace Microsoft.Extensions.DependencyInjection
{

    public static class FabronElasticSearchServiceCollectionExtensions
    {
        public static IServiceCollection UseElasticSearch(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<ElasticSearchOptions>(config);
            services.AddElasticSearchJobReporter();
            services.AddElasticSearchJobQuerier();
            return services;
        }

        public static IServiceCollection UseElasticSearch(this IServiceCollection services, Action<ElasticSearchOptions> configure)
        {
            services.Configure<ElasticSearchOptions>(configure);
            services.AddElasticSearchJobReporter();
            return services;
        }

        internal static IServiceCollection AddElasticSearchJobQuerier(this IServiceCollection services)
        {
            services.TryAddSingleton<IElasticClient>(sp =>
            {
                ElasticSearchOptions options = sp.GetRequiredService<IOptions<ElasticSearchOptions>>().Value;
                ConnectionSettings settings = new ConnectionSettings(new Uri(options.Server));
                ElasticClient client = new(settings);
                return client;
            });
            services.AddSingleton<IJobQuerier, ElasticSearchJobQuerier>();
            return services;
        }

        internal static IServiceCollection AddElasticSearchJobReporter(this IServiceCollection services)
        {
            services.TryAddSingleton<IElasticClient>(sp =>
            {
                ElasticSearchOptions options = sp.GetRequiredService<IOptions<ElasticSearchOptions>>().Value;
                ConnectionSettings settings = new ConnectionSettings(new Uri(options.Server));
                ElasticClient client = new(settings);
                return client;
            });
            services.AddSingleton<IJobIndexer, ElasticSearchJobIndexer>();
            return services;
        }
    }
}
