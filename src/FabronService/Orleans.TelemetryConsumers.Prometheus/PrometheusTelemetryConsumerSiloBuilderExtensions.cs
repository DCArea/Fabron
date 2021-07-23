// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
