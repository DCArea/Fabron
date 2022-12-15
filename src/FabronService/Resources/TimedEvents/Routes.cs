using FabronService.Resources.TimedEvents;

namespace Microsoft.AspNetCore.Routing;

public static partial class Routes
{
    public static IEndpointRouteBuilder MapTimedEvents(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/timedevents/{name}", TimedEvents.Schedule)
            .WithName($"{nameof(TimedEvents)}_{nameof(TimedEvents.Schedule)}")
            .RequireAuthorization();

        endpoints.MapGet("/timedevents/{name}", TimedEvents.Get)
            .WithName($"{nameof(TimedEvents)}_{nameof(TimedEvents.Get)}")
            .RequireAuthorization();

        endpoints.MapDelete("/timedevents/{name}", TimedEvents.Cancel)
            .WithName($"{nameof(TimedEvents)}_{nameof(TimedEvents.Cancel)}")
            .RequireAuthorization();

        endpoints.MapGet("/timedevents/{name}/ticker/status", TimedEvents.GetTickerStatus)
            .WithName($"{nameof(TimedEvents)}_{nameof(TimedEvents.GetTickerStatus)}")
            .RequireAuthorization();


        return endpoints;
    }
}
