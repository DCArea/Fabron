using System;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using TGH.Server.Grains;

namespace TGH.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseOrleans(siloBuilder =>
                {
                    siloBuilder
                        .UseLocalhostClustering()
                        .Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromMinutes(1))
                        .Configure<ClusterOptions>(opts =>
                        {
                            opts.ClusterId = "dev";
                            opts.ServiceId = "TGH";
                        })
                        .Configure<EndpointOptions>(opts =>
                        {
                            opts.AdvertisedIPAddress = IPAddress.Loopback;
                        })
                        .Configure<StatisticsOptions>(opts =>
                        {
                            opts.LogWriteInterval = TimeSpan.FromSeconds(10);
                            opts.CollectionLevel = Orleans.Runtime.Configuration.StatisticsLevel.Verbose;
                        })
                        .UseInMemoryReminderService()
                        .AddMemoryGrainStorage("JobStore")
                        .ConfigureApplicationParts(manager =>
                        {
                            manager.AddApplicationPart(typeof(JobGrain<,>).Assembly).WithReferences();
                        });
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
