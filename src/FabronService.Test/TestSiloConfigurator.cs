using System;
using FabronService.Commands;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.TestingHost;

namespace FabronService.Test
{
    public class TestSiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.Configure<MessagingOptions>(options=>{
                options.ResponseTimeout = TimeSpan.FromSeconds(5);
            });
            siloBuilder.UseInMemoryJobStore();
            siloBuilder.AddFabron(typeof(RequestWebAPI).Assembly);
        }
    }
}
