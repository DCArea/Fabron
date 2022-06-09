using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Fabron;
using Fabron.Core.CloudEvents;
using Microsoft.AspNetCore.Http;
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
        string? tenant = user.Identity?.Name;
        if (string.IsNullOrEmpty(tenant))
            return Results.Unauthorized();

        string key = KeyUtils.BuildPeriodicEventKey(tenant, name);
        var annotations = new Dictionary<string, string>
        {
            { "routing.fabron.io/destination", req.RoutingDestination}
        };

        await fabronClient.SchedulePeriodicEvent(
            key,
            req.Template,
            req.Period,
            req.NotBefore,
            req.ExpirationTime,
            req.Suspend,
            annotations: annotations);

        return Results.CreatedAtRoute("PeriodicEvents_Get", new { name });
    }
}

public record SchedulePeriodicEventRequest
(
    CloudEventTemplate<JsonElement> Template,
    TimeSpan Period,
    DateTimeOffset? NotBefore,
    DateTimeOffset? ExpirationTime,
    bool Suspend,
    string RoutingDestination
);
