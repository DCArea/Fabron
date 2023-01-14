using System.Security.Claims;
using System.Text.Json;
using Fabron;
using Microsoft.AspNetCore.Mvc;

namespace FabronService.Resources.PeriodicEvents;

public static partial class PeriodicEvents
{
    public static async Task<IResult> Schedule(
        [FromRoute] string name,
        [FromBody] SchedulePeriodicEventRequest req,
        ClaimsPrincipal user,
        [FromServices] IFabronClient fabronClient)
    {
        var tenant = user.Identity?.Name;
        if (string.IsNullOrEmpty(tenant))
        {
            return Results.Unauthorized();
        }

        var key = KeyUtils.BuildPeriodicEventKey(tenant, name);
        var extensions = new Dictionary<string, string>
        {
            { "routing.fabron.io/destination", req.RoutingDestination}
        };

        await fabronClient.SchedulePeriodicEvent(
            key,
            req.Data,
            req.Period,
            req.NotBefore,
            req.ExpirationTime,
            req.Suspend,
            extensions: extensions);

        return Results.CreatedAtRoute("PeriodicEvents_Get", new { name });
    }
}

public record SchedulePeriodicEventRequest
(
    string? Data,
    TimeSpan Period,
    DateTimeOffset? NotBefore,
    DateTimeOffset? ExpirationTime,
    bool Suspend,
    string RoutingDestination
);
