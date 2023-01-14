using System.Security.Claims;
using System.Text.Json;
using Fabron;
using Microsoft.AspNetCore.Mvc;

namespace FabronService.Resources.PeriodicEvents;

public static partial class PeriodicEvents
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

        var key = KeyUtils.BuildPeriodicEventKey(tenant, name);
        var cronEvent = await fabronClient.GetPeriodicEvent(key);
        return cronEvent is null ? Results.NotFound() : Results.Ok(cronEvent);
    }
}
