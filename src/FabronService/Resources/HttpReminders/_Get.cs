using System.Security.Claims;
using System.Threading.Tasks;
using Fabron;
using FabronService.Commands;
using FabronService.Resources.CronHttpReminders.Models;
using FabronService.Resources.HttpReminders.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FabronService.Resources.CronHttpReminders;

public static partial class HttpRemindersHandler
{
    public static async Task<IResult> Get(
        [FromRoute] string name,
        ClaimsPrincipal user,
        [FromServices] IJobManager jobManager)
    {
        string tenant = user.Identity!.Name!;
        var job = await jobManager.GetJob<RequestWebAPI, int>(name, tenant);
        return job is null ? Results.NotFound() : Results.Ok(job.ToResource(name));
    }
}
