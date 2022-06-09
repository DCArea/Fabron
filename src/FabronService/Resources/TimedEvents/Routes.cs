using FabronService.Resources.TimedEvents;
using Microsoft.AspNetCore.Builder;

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

        return endpoints;
    }
}
