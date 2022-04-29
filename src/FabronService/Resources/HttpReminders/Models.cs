
using System;

using Fabron.Contracts;
using Fabron.Models;
using FabronService.Commands;

namespace FabronService.Resources.HttpReminders.Models
{
    public record HttpReminder
    (
        string Name,
        RequestWebAPI Command,
        int? Result,
        DateTimeOffset? CreatedAt,
        DateTimeOffset? Schedule,
        DateTimeOffset? StartedAt,
        DateTimeOffset? FinishedAt,
        JobExecutionStatus Status,
        string? Reason
    );


    public static class HttpReminderExtensions
    {
        public static HttpReminder ToResource(this Job<RequestWebAPI, int> job, string reminderName)
            => new(
                reminderName,
                job.Spec.Command.Data,
                job.Status.Result,
                job.Metadata.CreationTimestamp,
                job.Spec.Schedule,
                null,
                null,
                job.Status.ExecutionStatus,
                job.Status.Reason
            );
    }
}
