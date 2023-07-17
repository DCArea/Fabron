using Fulu.Extensions.Telemetry;
using OpenTelemetry.Instrumentation.AspNetCore;

namespace OpenTelemetry.Trace;

public static class TracerProviderBuilderExtensions
{
    public static TracerProviderBuilder AddEncrichedAspNetCoreInstrumentation(
        this TracerProviderBuilder builder,
        Action<AspNetCoreInstrumentationOptions>? userOptions = null)
    {
        void finalOptions(AspNetCoreInstrumentationOptions options)
        {
            var requestEnrich = options.EnrichWithHttpRequest;
            options.EnrichWithHttpRequest = (activity, request) =>
            {
                AspNetCoreInstrumentationEnrichments.AttachTraceContextInHeader.Invoke(activity, request);
                requestEnrich?.Invoke(activity, request);
            };
            var responseEnrich = options.EnrichWithHttpResponse;
            options.EnrichWithHttpResponse = (activity, response) =>
            {
                AspNetCoreInstrumentationEnrichments.EnrichRouteName.Invoke(activity, response);
                responseEnrich?.Invoke(activity, response);
            };

            userOptions?.Invoke(options);
        }

        builder.ConfigureServices(services =>
        {
            services.Configure<AspNetCoreInstrumentationOptions>(finalOptions);
        });

        return builder.AddAspNetCoreInstrumentation();
    }
}
