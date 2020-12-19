using Fabron.Grains.TransientJob;
using Fabron.Mando;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans;
using System.Net;
using System;

namespace Fabron.Server
{
    public class FabronHostBuilder
    {
        private readonly IHostBuilder _hostBuilder;
        private readonly ISiloBuilder _siloBuilder;

        public FabronHostBuilder(IHostBuilder hostBuilder)
        {
            _hostBuilder = hostBuilder;
            ConfigureBase();
            _siloBuilder = (ISiloBuilder)_hostBuilder.Properties["SiloBuilder"];
        }

        public void ConfigureBase()
        {
            _hostBuilder
                .ConfigureServices((ctx, services) =>
                    {
                        services.AddScoped<IMediator, Mediator>();
                        services.AddSingleton<IJobManager, JobManager>();
                    })
                .UseOrleans(siloBuilder =>
                {
                    siloBuilder
                        // .Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(3))
                        .Configure<EndpointOptions>(opts =>
                        {
                            opts.AdvertisedIPAddress = IPAddress.Loopback;
                        })
                        .Configure<StatisticsOptions>(opts =>
                        {
                            opts.LogWriteInterval = TimeSpan.FromMinutes(1000);
                        })
                        .ConfigureApplicationParts(manager =>
                            manager.AddApplicationPart(typeof(TransientJobGrain).Assembly).WithReferences());
                });
        }

        public void UseLocalCluster(string clusterId = "dev")
        {
            _siloBuilder
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(opts =>
                {
                    opts.ClusterId = "dev";
                    opts.ServiceId = "Fabron";
                });
        }

        public void UseInMemoryStorage()
        {
            _siloBuilder
                .UseInMemoryReminderService()
                .AddMemoryGrainStorage("JobStore");
        }

        public void UseSQLServerStorage(string connectionString)
        {
            _siloBuilder
                .AddAdoNetGrainStorage("Fabron.JobStore", (AdoNetGrainStorageOptions opt) =>
                {
                    opt.Invariant = "Microsoft.Data.SqlClient";
                    opt.ConnectionString = connectionString;
                })
                .UseAdoNetReminderService((AdoNetReminderTableOptions opt) =>
                {
                    opt.Invariant = "Microsoft.Data.SqlClient";
                    opt.ConnectionString = connectionString;
                });
        }

    }

}
