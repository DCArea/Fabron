// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Fabron.Contracts;

using FabronService.Commands;

namespace FabronService.Resources.CronHttpReminders.Models
{
    public record CronHttpReminder
    (
        string Name,
        string Schedule,
        RequestWebAPI Command,
        IEnumerable<CronChildJob> ScheduledJobs,
        string? Reason
    );

    public record RegisterCronHttpReminderRequest
    (
        string Name,
        string Schedule,
        RequestWebAPI Command
    );

    public static class HttpReminderExtensions
    {
        public static CronHttpReminder ToResource(this CronJob<RequestWebAPI> job, string reminderName)
            => new(
                reminderName,
                job.Schedule,
                job.Command,
                job.ScheduledJobs,
                job.Reason
            );
    }
}
