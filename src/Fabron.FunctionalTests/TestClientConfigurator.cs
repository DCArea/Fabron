using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Orleans.TestingHost;

namespace Fabron.FunctionalTests
{
    internal class TestClientConfigurator : IClientBuilderConfigurator
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder) =>
            clientBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<IFabronClient, FabronClient>();
            });
    }
}
