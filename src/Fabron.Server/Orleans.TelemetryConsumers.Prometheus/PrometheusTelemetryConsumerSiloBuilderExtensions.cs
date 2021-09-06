
using Microsoft.Extensions.DependencyInjection;

using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.TelemetryConsumers.Prometheus;

namespace Microsoft.Extensions.Hosting
{
    public static class PrometheusTelemetryConsumerSiloBuilderExtensions
    {
        public static ISiloBuilder AddPrometheusTelemetryConsumer(this ISiloBuilder hostBuilder)
            => hostBuilder.ConfigureServices(services
                => services.Configure<TelemetryOptions>(options
                    => options.AddConsumer<PrometheusTelemetryConsumer>()
                )
            );
    }
}
