using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Fabron;
using FabronService.Commands;
using FabronService.Resources.CronHttpReminders.Models;
using FabronService.Resources.HttpReminders.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FabronService.Resources.CronHttpReminders;

public static partial class CronHttpRemindersHandler
{
    public static async Task<IResult> GetSchedules(
        [FromRoute] string name,
        [FromQuery] int? take,
        [FromQuery] int? skip,
        ClaimsPrincipal user,
        [FromServices] IFabronQuerier fabronQuerier)
    {
        string? tenant = user.Identity?.Name;
        if (string.IsNullOrEmpty(tenant))
            return Results.Unauthorized();

        if (take is null)
            take = 20;
        if (skip is null)
            skip = 0;

        var jobs = await fabronQuerier.FindJobByOwnerAsync<RequestWebAPI, int>(tenant, new()
        {
            Kind = "CronJob",
            Name = name
        }, skip.Value, take.Value);

        return Results.Ok(jobs.Select(job => job.ToResource(name)));
    }
}
