using FabronService.Resources;

namespace Microsoft.AspNetCore.Routing;

public static partial class Routes
{
    public static IEndpointRouteBuilder MapGenericTimerApis(this IEndpointRouteBuilder route)
    {
        var group = route.MapGroup("generictimers").RequireAuthorization();
        group.MapPut("{key}", GenericTimerApis.Schedule);
        group.MapGet("{key}", GenericTimerApis.Get);
        group.MapDelete("{key}", GenericTimerApis.Cancel);

        return route;
    }
}
