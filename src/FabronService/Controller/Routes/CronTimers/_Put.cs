using System.Security.Claims;
using Fabron;
using Microsoft.AspNetCore.Mvc;

namespace FabronService.Resources;

public static partial class CronTimerApis
{
    public static async Task<IResult> Schedule(
        [FromRoute] string key,
        [FromBody] ScheduleCronTimerRequest req,
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

        await fabronClient.ScheduleCronTimer(
            key,
            req.Data,
            req.Schedule,
            req.NotBefore,
            req.ExpirationTime,
            extensions: extensions);

        return Results.Ok(new { key });
    }
}

public record ScheduleCronTimerRequest
(
    string Name,
    string? Data,
    string Schedule,
    DateTimeOffset? NotBefore,
    DateTimeOffset? ExpirationTime,
    string RoutingDestination
);
