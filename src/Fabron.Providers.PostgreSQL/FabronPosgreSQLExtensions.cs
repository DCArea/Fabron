using System;
using Fabron.Stores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Hosting;

namespace Fabron.Providers.PostgreSQL
{
    public static class FabronPosgreSQLExtensions
    {
        const string Invariant = "Npgsql";

        public static IClientBuilder UsePostgreSQLClustering(this IClientBuilder client, string connectionString)
        {
            client.UseAdoNetClustering(options =>
            {
                options.Invariant = Invariant;
                options.ConnectionString = connectionString;
            });
            return client;
        }

        public static ISiloBuilder UsePosgreSQL(this ISiloBuilder silo, IConfiguration configuration)
        {
            silo.Configure<PostgreSQLOptions>(configuration);
            silo.UsePosgreSQL(configuration["ConnectionString"]);
            return silo;
        }

        public static ISiloBuilder UsePosgreSQL(this ISiloBuilder silo, Action<PostgreSQLOptions> configureOptions)
        {
            silo.Configure(configureOptions);
            var options = new PostgreSQLOptions();
            configureOptions(options);
            silo.UsePosgreSQL(options.ConnectionString);
            return silo;
        }
        public static ISiloBuilder UsePosgreSQL(this ISiloBuilder silo, string connectionString)
        {
            silo.UseAdoNetClustering(options =>
            {
                options.Invariant = Invariant;
                options.ConnectionString = connectionString;
            });
            silo.UseAdoNetReminderService(options =>
            {
                options.Invariant = Invariant;
                options.ConnectionString = connectionString;
            });
            silo.UsePostgreSQLEventStore();
            silo.UsePostgreSQLIndexStore();
            return silo;
        }

        public static ISiloBuilder UsePostgreSQLEventStore(this ISiloBuilder silo, Action<PostgreSQLOptions> configureOptions)
        {
            silo.Configure(configureOptions);
            silo.UsePostgreSQLEventStore();
            return silo;
        }
        public static ISiloBuilder UsePostgreSQLEventStore(this ISiloBuilder silo)
        {
            silo.ConfigureServices(services =>
            {
                services.AddPostgreSQLEventStore();
            });
            return silo;
        }

        public static ISiloBuilder UsePostgreSQLIndexStore(this ISiloBuilder silo, Action<PostgreSQLOptions> configureOptions)
        {
            silo.Configure(configureOptions);
            silo.UsePostgreSQLIndexStore();
            return silo;
        }
        public static ISiloBuilder UsePostgreSQLIndexStore(this ISiloBuilder silo)
        {
            silo.ConfigureServices(services =>
            {
                services.AddPostgreSQLIndexStore();
            });
            return silo;
        }

        public static IServiceCollection AddPostgreSQLEventStore(this IServiceCollection services)
        {
            services.AddSingleton<IJobEventStore, PostgreSQLJobEventStore>();
            services.AddSingleton<ICronJobEventStore, PostgreSQLCronJobEventStore>();
            return services;
        }

        public static IServiceCollection AddPostgreSQLIndexStore(this IServiceCollection services)
        {
            services.AddSingleton<IJobIndexer, PostgreSQLIndexer>();
            services.AddSingleton<IJobQuerier, PostgreSQLQuerier>();
            return services;
        }
    }

}
