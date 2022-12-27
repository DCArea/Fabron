
using System.Security.Claims;
using Fabron;
using Microsoft.AspNetCore.Mvc;

namespace FabronService.Resources.PeriodicEvents;

public static partial class PeriodicEvents
{
    public static async Task<IResult> GetTickerStatus(
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
        var tickerStatus = await fabronClient.GetPeriodicEventTickerStatus(key);
        return Results.Ok(tickerStatus);
    }
}
