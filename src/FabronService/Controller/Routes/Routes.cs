namespace Microsoft.AspNetCore.Routing;

public static partial class Routes
{
    public static IEndpointRouteBuilder MapRoutes(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapCronTimerApis();
        endpoints.MapGenericTimerApis();
        endpoints.MapPeriodicTimers();
        return endpoints;
    }
}
