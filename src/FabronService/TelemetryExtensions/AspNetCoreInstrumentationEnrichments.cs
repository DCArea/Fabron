using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;

namespace Fulu.Extensions.Telemetry;

public static class AspNetCoreInstrumentationEnrichments
{
    public static Action<Activity, HttpResponse> EnrichRouteName { get; } = (activity, response) =>
    {
        var context = response.HttpContext;

        activity.DisplayName = context.Features.Get<IEndpointFeature>()?.Endpoint is RouteEndpoint endpoint
            ? $"{context.Request.Scheme.ToUpperInvariant()} {context.Request.Method.ToUpperInvariant()} {endpoint.RoutePattern?.RawText}"
            : $"{context.Request.Scheme.ToUpperInvariant()} {context.Request.Method.ToUpperInvariant()}";
    };

    public static Action<Activity, HttpRequest> AttachTraceContextInHeader { get; } = (activity, request) =>
    {
        request.HttpContext.Response.Headers["trace-id"] = activity.TraceId.ToHexString();
    };
}
