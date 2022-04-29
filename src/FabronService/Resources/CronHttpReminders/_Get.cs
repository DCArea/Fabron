using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Fabron;
using FabronService.Commands;
using FabronService.Resources.CronHttpReminders.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FabronService.Resources.CronHttpReminders;

public static partial class CronHttpRemindersHandler
{
    public static async Task<IResult> Get(
        [FromRoute] string name,
        ClaimsPrincipal user,
        [FromServices] IJobManager jobManager)
    {
        string? tenant = user.Identity?.Name;
        if (string.IsNullOrEmpty(tenant))
            return Results.Unauthorized();

        var job = await jobManager.GetCronJob<RequestWebAPI>(name, tenant);
        return job is null ? Results.NotFound() : Results.Ok(job.ToResource());
    }
}
