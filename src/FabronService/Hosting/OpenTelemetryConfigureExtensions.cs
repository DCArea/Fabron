using System.Collections.Generic;
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
                .AddNpgsql()
                .AddSource("Microsoft.Orleans")
                .AddHttpClientInstrumentation()
                .AddGrpcClientInstrumentation()
                .AddOtlpExporter()
            );

        builder.Services.AddOpenTelemetryMetrics((builder) => builder
            .AddRuntimeMetrics()
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
