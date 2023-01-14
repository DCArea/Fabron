using System.Security.Claims;
using System.Text.Json;
using Fabron;
using Microsoft.AspNetCore.Mvc;

namespace FabronService.Resources.TimedEvents;

public static partial class TimedEvents
{
    public static async Task<IResult> Get(
        [FromRoute] string name,
        ClaimsPrincipal user,
        [FromServices] IFabronClient fabronClient)
    {
        var tenant = user.Identity?.Name;
        if (string.IsNullOrEmpty(tenant))
        {
            return Results.Unauthorized();
        }

        var key = KeyUtils.BuildTimedEventKey(tenant, name);
        var timedEvent = await fabronClient.GetTimedEvent(key);
        return timedEvent is null ? Results.NotFound() : Results.Ok(timedEvent);
    }
}
