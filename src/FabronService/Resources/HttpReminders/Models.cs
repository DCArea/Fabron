// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Fabron.Contracts;

using FabronService.Commands;

namespace FabronService.Resources.HttpReminders.Models
{
    public record HttpReminder
    (
        string Name,
        JobCommand<RequestWebAPI, int> Command,
        DateTime CreatedAt,
        DateTime Schedule,
        DateTime? StartedAt,
        DateTime? FinishedAt,
        JobStatus Status,
        string? Reason
    );

    public record RegisterHttpReminderRequest
    (
        string Name,
        DateTime Schedule,
        RequestWebAPI Command
    );

    public static class HttpReminderExtensions
    {
        public static HttpReminder ToResource(this Job<RequestWebAPI, int> job, string reminderName)
            => new(
                reminderName,
                job.Command,
                job.CreatedAt,
                job.ScheduledAt!.Value,
                job.StartedAt,
                job.FinishedAt,
                job.Status,
                job.Reason
            );
    }
}
