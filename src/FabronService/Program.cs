using System;
using FabronService.Commands;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Hosting;

namespace FabronService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseFabron(typeof(RequestWebAPI).Assembly, siloBuilder =>
                {
                    siloBuilder.UseLocalhostClustering();
                    siloBuilder.UseInMemoryJobStore();
                    siloBuilder.Configure<StatisticsOptions>(opts =>
                        {
                            opts.LogWriteInterval = TimeSpan.FromMinutes(1000);
                        });
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
