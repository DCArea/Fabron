
using System;
using Fabron;

using Microsoft.Extensions.DependencyInjection;

using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.TestingHost;

namespace FabronService.FunctionalTests
{
    public class TestSiloConfigurator : ISiloConfigurator
    {
        public virtual void ConfigureServices(Microsoft.Extensions.Hosting.HostBuilderContext context, IServiceCollection services) => services.AddHttpClient();

        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.Configure<MessagingOptions>(options =>
            {
                options.ResponseTimeout = TimeSpan.FromSeconds(5);
            });
            siloBuilder.UseInMemory();
            siloBuilder.ConfigureServices(ConfigureServices);
            siloBuilder.AddFabron();
        }
    }
}
