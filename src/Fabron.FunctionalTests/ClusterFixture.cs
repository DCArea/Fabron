using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.TestingHost;

namespace Fabron.FunctionalTests
{
    public class DefaultClusterFixture : ClusterFixture<TestSiloConfigurator>
    { }

    public class ClusterFixture<TSiloConfigurator> : Xunit.IAsyncLifetime
        where TSiloConfigurator : ISiloConfigurator, new()
    {

        public TestCluster HostedCluster { get; private set; } = default!;

        public IGrainFactory GrainFactory => HostedCluster.GrainFactory;

        public IClusterClient Client => HostedCluster.Client;

        public ILogger Logger { get; private set; } = default!;

        public IServiceProvider ClusterServices => ((InProcessSiloHandle)HostedCluster.Primary!).SiloHost.Services;

        public virtual async Task InitializeAsync()
        {
            var builder = new TestClusterBuilder();
            builder.ConfigureHostConfiguration(config =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Logging:LogLevel:Default", "Error" },
                    // { "Logging:LogLevel:Fabron", "Debug" }
                });
            });
            builder.AddSiloBuilderConfigurator<TSiloConfigurator>();
            builder.AddClientBuilderConfigurator<TestClientConfigurator>();

            var testCluster = builder.Build();
            if (testCluster.Primary == null)
            {
                await testCluster.DeployAsync().ConfigureAwait(false);
            }

            HostedCluster = testCluster;
            Logger = Client.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Application");
        }

        public virtual async Task DisposeAsync()
        {
            var cluster = HostedCluster;
            if (cluster is null)
            {
                return;
            }

            try
            {
                await cluster.StopAllSilosAsync().ConfigureAwait(false);
            }
            finally
            {
                await cluster.DisposeAsync().ConfigureAwait(false);
            }
        }

    }
}
