using System;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans.TestingHost;

namespace FabronService.Test
{
    public class WAF : WebApplicationFactory<Startup>
    {
        public TestCluster TestCluster { get; private set; }
        public JsonSerializerOptions JsonSerializerOptions =>
            Server.Services.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
        public WAF()
        {
            TestCluster = CreateTestCluster();
        }

        protected override void Dispose(bool disposing)
        {
            TestCluster.StopAllSilos();
            base.Dispose(disposing);
        }

        protected override IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices((ctx, services) =>
                {
                    services.AddFabronCore();
                    services.AddSingleton(TestCluster.Client);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
        }

        public TestCluster CreateTestCluster()
        {
            var builder = new TestClusterBuilder();
            builder.Options.ServiceId = Guid.NewGuid().ToString();
            builder.AddSiloBuilderConfigurator<TestSiloConfigurator>();
            var cluster = builder.Build();
            cluster.Deploy();
            return cluster;
        }
    }
}
