using System.Security.Claims;
using System.Text.Json;
using Fabron;
using Microsoft.AspNetCore.Mvc;

namespace FabronService.Resources;

public static partial class GenericTimerApis
{
    public static async Task<IResult> Schedule(
        [FromRoute] string key,
        [FromBody] ScheduleGenericTimerRequest req,
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
        await fabronClient.ScheduleGenericTimer(
            key,
            req.Data,
            req.Schedule,
            extensions: extensions);

        return Results.Ok(new { key });
    }
}

public record ScheduleGenericTimerRequest
(
    DateTimeOffset Schedule,
    string? Data,
    string RoutingDestination
);
