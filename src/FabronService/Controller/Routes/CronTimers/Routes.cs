using FabronService.Resources;

namespace Microsoft.AspNetCore.Routing;

public static partial class Routes
{
    public static IEndpointRouteBuilder MapCronTimerApis(this IEndpointRouteBuilder route)
    {
        var group = route.MapGroup("crontimers").RequireAuthorization();
        group.MapPut("{key}", CronTimerApis.Schedule);
        group.MapGet("{key}", CronTimerApis.Get);
        group.MapDelete("{key}", CronTimerApis.Delete);
        return route;
    }
}
