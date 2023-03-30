
using System.Security.Claims;
using Fabron;
using Microsoft.AspNetCore.Mvc;

namespace FabronService.Resources;

public static partial class CronTimerApis
{
    public static async Task<IResult> GetTickerStatus(
        [FromRoute] string key,
        ClaimsPrincipal user,
        [FromServices] IFabronClient fabronClient)
    {
        var tenant = user.Identity?.Name;
        if (string.IsNullOrEmpty(tenant))
        {
            return Results.Unauthorized();
        }

        key = KeyUtils.BuildTimerKey(tenant, key);
        var tickerStatus = await fabronClient.GetCronTimerTickerStatus(key);
        return Results.Ok(tickerStatus);
    }
}
