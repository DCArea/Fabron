using System.Security.Claims;
using System.Threading.Tasks;
using Fabron;
using Microsoft.AspNetCore.Http;
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
        string? tenant = user.Identity?.Name;
        if (string.IsNullOrEmpty(tenant))
            return Results.Unauthorized();

        string key = KeyUtils.BuildTimedEventKey(tenant, name);
        await fabronClient.CancelCronEvent(key);

        return Results.NoContent();
    }
}
