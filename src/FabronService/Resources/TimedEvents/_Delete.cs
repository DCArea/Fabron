using System.Security.Claims;
using Fabron;
using Microsoft.AspNetCore.Mvc;

namespace FabronService.Resources.TimedEvents;

public static partial class TimedEvents
{
    public static async Task<IResult> Cancel(
        [FromRoute] string name,
        [FromBody] ScheduleTimedEventRequest req,
        ClaimsPrincipal user,
        [FromServices] IFabronClient fabronClient)
    {
        var tenant = user.Identity?.Name;
        if (string.IsNullOrEmpty(tenant))
        {
            return Results.Unauthorized();
        }

        var key = KeyUtils.BuildTimedEventKey(tenant, name);
        await fabronClient.CancelCronEvent(key);

        return Results.NoContent();
    }
}
