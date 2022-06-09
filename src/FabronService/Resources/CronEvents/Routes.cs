using FabronService.Resources.CronEvents;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Routing;

public static partial class Routes
{
    public static IEndpointRouteBuilder MapCronEvents(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/cronevents/{name}", CronEvents.Schedule)
            .WithName($"{nameof(CronEvents)}_{nameof(CronEvents.Schedule)}")
            .RequireAuthorization();

        endpoints.MapGet("/cronevents/{name}", CronEvents.Get)
            .WithName($"{nameof(CronEvents)}_{nameof(CronEvents.Get)}")
            .RequireAuthorization();

        endpoints.MapDelete("/cronevents/{name}", CronEvents.Delete)
            .WithName($"{nameof(CronEvents)}_{nameof(CronEvents.Delete)}")
            .RequireAuthorization();

        return endpoints;
    }
}
