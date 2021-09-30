using System;
using Cassandra;
using Fabron.Stores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Hosting;
using OrleansCassandraUtils;
using OrleansCassandraUtils.Utils;

namespace Fabron.Providers.Cassandra
{
    public static class FabronCassandraExtensions
    {
        // public static IClientBuilder UseCassandraClustering(this IClientBuilder client, string connectionString)
        // {
        //     client.UseCassandraClustering(connectionString);
        //     return client;
        // }

        public static ISiloBuilder UseCassandra(this ISiloBuilder silo, IConfiguration configuration)
        {
            silo.Configure<CassandraOptions>(configuration);
            silo.UseCassandra(configuration["ConnectionString"]);
            return silo;
        }

        public static ISiloBuilder UseCassandra(this ISiloBuilder silo, Action<CassandraOptions> configureOptions)
        {
            silo.Configure(configureOptions);
            var options = new CassandraOptions();
            configureOptions(options);
            silo.UseCassandra(options.ConnectionString);
            return silo;
        }
        public static ISiloBuilder UseCassandra(this ISiloBuilder silo, string connectionString)
        {
            silo.UseCassandraClustering(options =>
            {
                options.ConnectionString = connectionString;
            });
            silo.UseCassandraReminderService(options =>
            {
                options.ConnectionString = connectionString;
            });
            silo.UseCassandraEventStores();
            return silo;
        }

        public static ISiloBuilder UseCassandraEventStores(this ISiloBuilder silo, Action<CassandraOptions> configureOptions)
        {
            silo.Configure(configureOptions);
            silo.ConfigureServices(services =>
            {
                services.TryAddSingleton<ISession>(sp =>
                {
                    string? connectionString = sp.GetRequiredService<IOptions<CassandraOptions>>().Value.ConnectionString;
                    var session = CassandraSessionFactory.CreateSession(connectionString).GetAwaiter().GetResult();
                    return session;
                });
            });
            silo.UseCassandraEventStores();
            return silo;
        }
        public static ISiloBuilder UseCassandraEventStores(this ISiloBuilder silo)
        {
            silo.ConfigureServices(services =>
            {
                services.AddSingleton<IJobEventStore, CassandraJobEventStore>();
                services.AddSingleton<ICronJobEventStore, CassandraCronJobEventStore>();
            });
            return silo;
        }
    }

}
