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
    public static async Task<IResult> Register(
        [FromRoute] string name,
        [FromBody] RegisterCronHttpReminderRequest req,
        ClaimsPrincipal user,
        [FromServices] IJobManager jobManager)
    {
        string? tenant = user.Identity?.Name;
        if (string.IsNullOrEmpty(tenant))
            return Results.Unauthorized();

        var job = await jobManager.ScheduleCronJob(
            name,
            tenant,
            req.Command,
            req.Schedule,
            req.NotBefore,
            req.ExpirationTime);
        CronHttpReminder? reminder = job.ToResource();
        return Results.CreatedAtRoute("CronHttpReminders_Get", new { name = reminder.Name }, reminder);
    }
}

public record RegisterCronHttpReminderRequest
(
    string Name,
    string Schedule,
    DateTimeOffset? NotBefore,
    DateTimeOffset? ExpirationTime,
    RequestWebAPI Command
);
