
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Fabron;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.TestingHost;

using Xunit;

namespace FabronService.FunctionalTests
{
    public class WAF : WAF<TestSiloConfigurator>
    {
    }

    public class WAF<TSiloConfigurator> : WebApplicationFactory<TestStartup>, IAsyncLifetime
        where TSiloConfigurator : TestSiloConfigurator, new()
    {
        public TestCluster TestCluster { get; private set; } = default!;
        public JsonSerializerOptions JsonSerializerOptions =>
            Server.Services.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;

        public async Task InitializeAsync() => TestCluster = await CreateTestClusterAsync();

        Task IAsyncLifetime.DisposeAsync() => TestCluster.StopAllSilosAsync();

        protected override IHostBuilder CreateHostBuilder()
        {
            string assemblyName = typeof(TestStartup).Assembly.GetName().Name!;
            string settingSuffix = assemblyName.ToUpperInvariant().Replace(".", "_");
            string envName = $"ASPNETCORE_TEST_CONTENTROOT_{settingSuffix}";
            return Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string> { { "ApiKey", "debug" } });
                })
                .ConfigureServices((ctx, services) =>
                {
                    services.AddSingleton<IJobManager, JobManager>();
                    services.AddSingleton(TestCluster);
                    services.AddSingleton(TestCluster.Client);
                    services.AddSingleton<IGrainFactory>(TestCluster.Client);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseSolutionRelativeContentRoot("src/FabronService");
                    string contentRoot = webBuilder.GetSetting(WebHostDefaults.ContentRootKey) ?? throw new InvalidOperationException("content root not valid");
                    Environment.SetEnvironmentVariable(envName, contentRoot);
                    webBuilder.UseStartup<TestStartup>();
                });
        }

        public async Task<TestCluster> CreateTestClusterAsync()
        {
            TestClusterBuilder builder = new();
            builder.Options.InitialSilosCount = 1;
            builder.Options.ServiceId = Guid.NewGuid().ToString();
            builder.AddSiloBuilderConfigurator<TSiloConfigurator>();
            TestCluster? cluster = builder.Build();
            await cluster.DeployAsync();
            return cluster;
        }

    }
}
