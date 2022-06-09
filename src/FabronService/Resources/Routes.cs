namespace Microsoft.AspNetCore.Routing;

public static partial class Routes
{
    public static IEndpointRouteBuilder MapRoutes(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapCronEvents();
        endpoints.MapTimedEvents();
        endpoints.MapPeriodicEvents();
        return endpoints;
    }
}
