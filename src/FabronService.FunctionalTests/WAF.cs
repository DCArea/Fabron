
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fabron;
using FakeItEasy;
using MartinCostello.Logging.XUnit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace FabronService.FunctionalTests
{
    public class WAF : WebApplicationFactory<Program>
    {
        private readonly IMessageSink _sink;
        public WAF(IMessageSink sink)
        {
            _sink = sink;
        }

        public JsonSerializerOptions JsonSerializerOptions =>
            Server.Services.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder = builder.ConfigureAppConfiguration(config =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string> { { "ApiKey", "debug" } });
                })
                .ConfigureServices((ctx, services) =>
                {
                    services.RemoveAll<IFabronClient>();
                    services.AddSingleton(A.Fake<IFabronClient>());
                    var orleansServices = services
                        .Where(svc => svc.ServiceType.Assembly.FullName!.StartsWith("Orleans.")
                            || svc.ImplementationType is not null && svc.ImplementationType.Assembly.FullName!.StartsWith("Orleans."))
                        .ToList();
                    foreach (var orleansService in orleansServices)
                        services.Remove(orleansService);
                });
            builder.ConfigureLogging(logging =>
            {
                logging.AddXUnit(_sink);
            });
            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                // services.AddSingleton(A.Fake<IFabronClient>());
            });
            base.ConfigureWebHost(builder);
        }
    }
}
