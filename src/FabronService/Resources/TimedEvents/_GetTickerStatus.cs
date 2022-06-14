
using System.Security.Claims;
using System.Threading.Tasks;
using Fabron;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FabronService.Resources.TimedEvents;

public static partial class TimedEvents
{
    public static async Task<IResult> GetTickerStatus(
        [FromRoute] string name,
        ClaimsPrincipal user,
        [FromServices] IFabronClient fabronClient)
    {
        string? tenant = user.Identity?.Name;
        if (string.IsNullOrEmpty(tenant))
            return Results.Unauthorized();

        string key = KeyUtils.BuildTimedEventKey(tenant, name);
        var tickerStatus = await fabronClient.GetTimedEventTickerStatus(key);
        return Results.Ok(tickerStatus);
    }
}
