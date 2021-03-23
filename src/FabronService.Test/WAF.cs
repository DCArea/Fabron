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
using Xunit.Abstractions;
using Xunit.Sdk;

namespace FabronService.Test
{
    public class WAF : WAF<TestSiloConfigurator>
    {
        public WAF(IMessageSink? output = null) : base(output)
        {
        }
    }

    public class WAF<TSiloConfigurator> : WebApplicationFactory<Startup>, IAsyncLifetime
        where TSiloConfigurator : TestSiloConfigurator, new()
    {
        private readonly IMessageSink? _output;

        public TestCluster TestCluster { get; private set; } = default!;
        public JsonSerializerOptions JsonSerializerOptions =>
            Server.Services.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;

        public WAF(IMessageSink? output = null)
        {
            _output = output;
        }


        public async Task InitializeAsync()
        {
            _output?.OnMessage(new DiagnosticMessage("InitializeAsync"));
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
                    services.AddSingleton(TestCluster);
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
            builder.Options.InitialSilosCount = 1;
            builder.Options.ServiceId = Guid.NewGuid().ToString();
            builder.AddSiloBuilderConfigurator<TSiloConfigurator>();
            var cluster = builder.Build();
            await cluster.DeployAsync();
            return cluster;
        }

    }
}
