using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Fabron;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FabronService.Resources.CronEvents;

public static partial class CronEvents
{
    public static async Task<IResult> Get(
        [FromRoute] string name,
        ClaimsPrincipal user,
        [FromServices] IFabronClient fabronClient)
    {
        string? tenant = user.Identity?.Name;
        if (string.IsNullOrEmpty(tenant))
            return Results.Unauthorized();

        string key = KeyUtils.BuildCronEventKey(tenant, name);
        var cronEvent = await fabronClient.GetCronEvent<JsonElement>(key);
        if (cronEvent is null)
            return Results.NotFound();
        return Results.Ok(cronEvent);
    }
}
