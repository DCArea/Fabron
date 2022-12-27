using System.Security.Claims;
using System.Text.Json;
using Fabron;
using Fabron.CloudEvents;
using Microsoft.AspNetCore.Mvc;

namespace FabronService.Resources.CronEvents;

public static partial class CronEvents
{
    public static async Task<IResult> Schedule(
        [FromRoute] string name,
        [FromBody] ScheduleCronEventRequest req,
        ClaimsPrincipal user,
        [FromServices] IFabronClient fabronClient)
    {
        var tenant = user.Identity?.Name;
        if (string.IsNullOrEmpty(tenant))
        {
            return Results.Unauthorized();
        }

        var key = KeyUtils.BuildCronEventKey(tenant, name);
        var annotations = new Dictionary<string, string>
        {
            { "routing.fabron.io/destination", req.RoutingDestination}
        };

        await fabronClient.ScheduleCronEvent(
            key,
            req.Schedule,
            req.Template,
            req.NotBefore,
            req.ExpirationTime,
            req.Suspend,
            annotations: annotations);

        return Results.CreatedAtRoute("CronEvents_Get", new { name });
    }
}

public record ScheduleCronEventRequest
(
    string Name,
    string Schedule,
    CloudEventTemplate<JsonElement> Template,
    DateTimeOffset? NotBefore,
    DateTimeOffset? ExpirationTime,
    bool Suspend,
    string RoutingDestination
);
