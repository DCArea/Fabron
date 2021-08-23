// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Fabron.Contracts;
using Fabron.Models;
using FabronService.Commands;

namespace FabronService.Resources.CronHttpReminders.Models
{
    public record CronHttpReminder
    (
        string Name,
        string Schedule,
        RequestWebAPI Command,
        IEnumerable<JobItem> ScheduledJobs,
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
        public static CronHttpReminder ToResource(this CronJob<RequestWebAPI> cronJob, string reminderName)
            => new(
                reminderName,
                cronJob.Spec.Schedule,
                cronJob.Spec.CommandData,
                cronJob.Status.Jobs,
                cronJob.Status.Reason
            );
    }
}
