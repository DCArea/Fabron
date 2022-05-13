using System;
using Fabron.Store;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Hosting;

namespace Fabron.Providers.PostgreSQL
{
    public static class FabronPosgreSQLExtensions
    {
        const string Invariant = "Npgsql";

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
            // silo.UsePostgreSQLEventStore();
            // silo.UsePostgreSQLIndexStore();
            return silo;
        }

        public static FabronClientBuilder UsePostgreSQLClustering(this FabronClientBuilder client, string connectionString)
        {
            client.ConfigureOrleansClient((ctx, cb) =>
            {
                cb.UseAdoNetClustering(options =>
                {
                    options.Invariant = Invariant;
                    options.ConnectionString = connectionString;
                });
            });
            return client;
        }

        public static FabronServerBuilder UsePostgreSQL(this FabronServerBuilder server, string connectionString)
        {
            server.UsePostgreSQLClustering(connectionString);
            server.UsePosgreSQLReminder(connectionString);
            server.UsePosgreSQLStore(connectionString);

            return server;
        }


        public static FabronServerBuilder UsePostgreSQLClustering(this FabronServerBuilder server, string connectionString)
        {
            server.ConfigureOrleans((ctx, sb) =>
            {
                sb.UseAdoNetClustering(options =>
                {
                    options.Invariant = Invariant;
                    options.ConnectionString = connectionString;
                });
            });
            return server;
        }

        public static FabronServerBuilder UsePosgreSQLReminder(this FabronServerBuilder server, string connectionString)
        {
            server.ConfigureOrleans((ctx, sb) =>
            {
                sb.UseAdoNetReminderService(options =>
                {
                    options.Invariant = Invariant;
                    options.ConnectionString = connectionString;
                });
            });
            return server;
        }

        public static FabronServerBuilder UsePosgreSQLStore(this FabronServerBuilder server, string connectionString)
        {
            server.HostBuilder.ConfigureServices((ctx, services) =>
            {
                services.Configure<PostgreSQLOptions>(options =>
                {
                    options.ConnectionString = connectionString;
                });
                services.AddSingleton<IJobStore, PostgreSQLJobStore>();
                services.AddSingleton<ICronJobStore, PostgreSQLCronJobStore>();
            });
            return server;
        }
    }
}
