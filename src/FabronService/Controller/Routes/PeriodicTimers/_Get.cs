using System.Security.Claims;
using Fabron;
using Microsoft.AspNetCore.Mvc;

namespace FabronService.Resources;

public static partial class PeriodicTimerApis
{
    public static async Task<IResult> Get(
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
        var timer = await fabronClient.GetPeriodicTimer(key);
        return timer is null ? Results.NotFound() : Results.Ok(timer);
    }
}
