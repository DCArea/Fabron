using System.Security.Claims;
using System.Text.Json;
using Fabron;
using Microsoft.AspNetCore.Mvc;

namespace FabronService.Resources.TimedEvents;

public static partial class TimedEvents
{
    public static async Task<IResult> Schedule(
        [FromRoute] string name,
        [FromBody] ScheduleTimedEventRequest req,
        ClaimsPrincipal user,
        [FromServices] IFabronClient fabronClient)
    {
        var tenant = user.Identity?.Name;
        if (string.IsNullOrEmpty(tenant))
        {
            return Results.Unauthorized();
        }

        var key = KeyUtils.BuildTimedEventKey(tenant, name);
        var extensions = new Dictionary<string, string>
        {
            { "routing.fabron.io/destination", req.RoutingDestination}
        };
        await fabronClient.ScheduleTimedEvent(
            key,
            req.Schedule,
            req.Data,
            extensions: extensions);

        return Results.CreatedAtRoute("TimedEvents_Get", new { name });
    }
}

public record ScheduleTimedEventRequest
(
    DateTimeOffset Schedule,
    JsonElement Data,
    string RoutingDestination
);
