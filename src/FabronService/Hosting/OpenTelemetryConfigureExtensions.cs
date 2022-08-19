using System.Collections.Generic;
using Fabron.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace FabronService.Hosting;

public static class OpenTelemetryConfigureExtensions
{
    public static WebApplicationBuilder ConfigureOpenTelemetry(this WebApplicationBuilder builder)
    {
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>{
            { "Logging:Console:FormatterOptions:IncludeScopes", "true" },
        });
        builder.Logging.AddEnrichedJsonConsole();
        builder.Services
            .AddOpenTelemetryTracing(options => options
                .AddEncrichedAspNetCoreInstrumentation()
                // .AddNpgsql()
                .AddSource("Microsoft.Orleans")
                .AddSource("Fabron")
                .AddHttpClientInstrumentation()
                .AddGrpcClientInstrumentation()
                .SetSampler<MySampler>()
                .AddOtlpExporter()
            );

        builder.Services.AddOpenTelemetryMetrics((builder) => builder
            .AddRuntimeMetrics()
            .AddMeter("Fabron")
            .AddMeter("Orleans")
            .AddPrometheusExporter()
        );

        return builder;
    }

    public static WebApplication UseOpenTelemetry(this WebApplication app)
    {
        app.UseOpenTelemetryPrometheusScrapingEndpoint();
        return app;
    }
}

internal class MySampler : Sampler
{
    private static readonly SamplingResult s_recordAndSample = new(SamplingDecision.RecordAndSample);
    private static readonly SamplingResult s_drop = new(SamplingDecision.Drop);
    public override SamplingResult ShouldSample(in SamplingParameters param)
    {
        if (param.Name == "Health checks"
            || param.Name == "HTTP GET"
            || param.Name.StartsWith("IDeploymentLoadPublisher")
            || param.Name.StartsWith("IMembershipService")
            || param.Name.StartsWith("ISiloManifestSystemTarget")
            || param.Name.StartsWith("IDhtGrainDirectory")
            || param.Name.StartsWith("IRemoteClientDirectory")
            || param.Name.StartsWith("IRemoteGrainDirectory"))
        {
            return s_drop;
        }

        return s_recordAndSample;
    }
}
