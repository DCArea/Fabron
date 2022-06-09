using System.Security.Claims;
using System.Threading.Tasks;
using Fabron;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FabronService.Resources.PeriodicEvents;

public static partial class PeriodicEvents
{
    public static async Task<IResult> Delete(
        [FromRoute] string name,
        ClaimsPrincipal user,
        [FromServices] IFabronClient fabronClient)
    {
        string? tenant = user.Identity?.Name;
        if (string.IsNullOrEmpty(tenant))
            return Results.Unauthorized();

        string key = KeyUtils.BuildPeriodicEventKey(tenant, name);
        await fabronClient.CancelPeriodicEvent(key);

        return Results.NoContent();
    }
}
