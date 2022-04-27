
using System;
using Fabron;

using Microsoft.Extensions.DependencyInjection;

using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Serialization;
using Orleans.TestingHost;

namespace FabronService.FunctionalTests
{
    public class TestSiloConfigurator : ISiloConfigurator
    {
        public virtual void ConfigureServices(IServiceCollection services) => services.AddHttpClient();

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
