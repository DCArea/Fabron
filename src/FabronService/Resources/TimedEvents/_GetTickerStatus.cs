
using System.Security.Claims;
using Fabron;
using Microsoft.AspNetCore.Mvc;

namespace FabronService.Resources.TimedEvents;

public static partial class TimedEvents
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

        var key = KeyUtils.BuildTimedEventKey(tenant, name);
        var tickerStatus = await fabronClient.GetTimedEventTickerStatus(key);
        return Results.Ok(tickerStatus);
    }
}
