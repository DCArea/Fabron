using FabronService.Resources;

namespace Microsoft.AspNetCore.Routing;

public static partial class Routes
{
    public static IEndpointRouteBuilder MapPeriodicTimers(this IEndpointRouteBuilder route)
    {
        var group = route.MapGroup("periodictimers").RequireAuthorization();
        group.MapPut("{key}", PeriodicTimerApis.Schedule);
        group.MapGet("{key}", PeriodicTimerApis.Get);
        group.MapDelete("{key}", PeriodicTimerApis.Delete);
        return route;
    }
}
