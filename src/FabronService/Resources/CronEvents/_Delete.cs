using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Fabron;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FabronService.Resources.CronEvents;

public static partial class CronEvents
{
    public static async Task<IResult> Delete(
        [FromRoute] string name,
        ClaimsPrincipal user,
        [FromServices] IFabronClient fabronClient)
    {
        string? tenant = user.Identity?.Name;
        if (string.IsNullOrEmpty(tenant))
            return Results.Unauthorized();

        string key = KeyUtils.BuildCronEventKey(tenant, name);
        await fabronClient.CancelCronEvent(key);

        return Results.NoContent();
    }
}
