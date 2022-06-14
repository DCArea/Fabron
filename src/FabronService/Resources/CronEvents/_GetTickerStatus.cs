
using System.Security.Claims;
using System.Threading.Tasks;
using Fabron;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FabronService.Resources.CronEvents;

public static partial class CronEvents
{
    public static async Task<IResult> GetTickerStatus(
        [FromRoute] string name,
        ClaimsPrincipal user,
        [FromServices] IFabronClient fabronClient)
    {
        string? tenant = user.Identity?.Name;
        if (string.IsNullOrEmpty(tenant))
            return Results.Unauthorized();

        string key = KeyUtils.BuildCronEventKey(tenant, name);
        var tickerStatus = await fabronClient.GetCronEventTickerStatus(key);
        return Results.Ok(tickerStatus);
    }
}
