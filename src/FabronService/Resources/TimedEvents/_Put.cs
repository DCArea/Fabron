using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Fabron;
using Fabron.CloudEvents;
using Microsoft.AspNetCore.Http;
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
        string? tenant = user.Identity?.Name;
        if (string.IsNullOrEmpty(tenant))
            return Results.Unauthorized();

        string key = KeyUtils.BuildTimedEventKey(tenant, name);
        var annotations = new Dictionary<string, string>
        {
            { "routing.fabron.io/destination", req.RoutingDestination}
        };
        await fabronClient.ScheduleTimedEvent(
            key,
            req.Schedule,
            req.Template,
            annotations: annotations);

        return Results.CreatedAtRoute("TimedEvents_Get", new { name });
    }
}

public record ScheduleTimedEventRequest
(
    DateTimeOffset Schedule,
    CloudEventTemplate<JsonElement> Template,
    string RoutingDestination
);
