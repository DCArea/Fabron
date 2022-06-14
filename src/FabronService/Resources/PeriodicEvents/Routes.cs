using FabronService.Resources.PeriodicEvents;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Routing;

public static partial class Routes
{
    public static IEndpointRouteBuilder MapPeriodicEvents(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/periodicevents/{name}", PeriodicEvents.Schedule)
            .WithName($"{nameof(PeriodicEvents)}_{nameof(PeriodicEvents.Schedule)}")
            .RequireAuthorization();

        endpoints.MapGet("/periodicevents/{name}", PeriodicEvents.Get)
            .WithName($"{nameof(PeriodicEvents)}_{nameof(PeriodicEvents.Get)}")
            .RequireAuthorization();

        endpoints.MapDelete("/periodicevents/{name}", PeriodicEvents.Delete)
            .WithName($"{nameof(PeriodicEvents)}_{nameof(PeriodicEvents.Delete)}")
            .RequireAuthorization();

        endpoints.MapGet("/periodicevents/{name}/ticker/status", PeriodicEvents.GetTickerStatus)
            .WithName($"{nameof(PeriodicEvents)}_{nameof(PeriodicEvents.GetTickerStatus)}")
            .RequireAuthorization();

        return endpoints;
    }
}
