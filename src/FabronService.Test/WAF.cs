using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans.TestingHost;
using Xunit;

namespace FabronService.Test
{
    public class WAF : WebApplicationFactory<Startup>, IAsyncLifetime
    {
        public TestCluster TestCluster { get; private set; } = default!;
        public JsonSerializerOptions JsonSerializerOptions =>
            Server.Services.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;

        public async Task InitializeAsync()
        {
            TestCluster = await CreateTestClusterAsync();
        }

        public async Task DisposeAsync()
        {
            await TestCluster.StopAllSilosAsync();
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

        public async Task<TestCluster> CreateTestClusterAsync()
        {
            var builder = new TestClusterBuilder();
            builder.Options.ServiceId = Guid.NewGuid().ToString();
            builder.AddSiloBuilderConfigurator<TestSiloConfigurator>();
            var cluster = builder.Build();
            await cluster.DeployAsync();
            return cluster;
        }

    }
}
