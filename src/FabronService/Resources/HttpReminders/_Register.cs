using System;
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
    public static async Task<IResult> Register(
        [FromRoute] string name,
        [FromBody] RegisterHttpReminderRequest req,
        ClaimsPrincipal user,
        [FromServices] IJobManager jobManager)
    {
        string tenant = user.Identity!.Name!;
        var job = await jobManager.ScheduleJob<RequestWebAPI, int>(
            req.Name,
            tenant,
            req.Command,
            req.Schedule);
        HttpReminder reminder = job.ToResource(req.Name);
        return Results.CreatedAtRoute("HttpReminders_Get", new { name = reminder.Name }, reminder);
    }
}

public record RegisterHttpReminderRequest
(
    string Name,
    DateTimeOffset Schedule,
    RequestWebAPI Command
);
