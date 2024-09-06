using System.Security.Claims;
using Fabron;
using Microsoft.AspNetCore.Mvc;

namespace FabronService.Resources;

public static partial class PeriodicTimerApis
{
    public static async Task<IResult> Schedule(
        [FromRoute] string key,
        [FromBody] SchedulePeriodicTimerRequest req,
        ClaimsPrincipal user,
        [FromServices] IFabronClient fabronClient)
    {
        var tenant = user.Identity?.Name;
        if (string.IsNullOrEmpty(tenant))
        {
            return Results.Unauthorized();
        }

        key = KeyUtils.BuildTimerKey(tenant, key);
        var extensions = new Dictionary<string, string>
        {
            { "routing.fabron.io/destination", req.RoutingDestination}
        };

        await fabronClient.SchedulePeriodicTimer(
            key,
            req.Data,
            req.Period,
            req.NotBefore,
            req.ExpirationTime,
            extensions: extensions);

        return Results.Ok(new { key });
    }
}

public record SchedulePeriodicTimerRequest
(
    string? Data,
    TimeSpan Period,
    DateTimeOffset? NotBefore,
    DateTimeOffset? ExpirationTime,
    string RoutingDestination
);
