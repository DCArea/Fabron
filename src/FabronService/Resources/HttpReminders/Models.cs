
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
        DateTime CreatedAt,
        DateTime Schedule,
        DateTime? StartedAt,
        DateTime? FinishedAt,
        ExecutionStatus Status,
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
                job.Spec.CommandData,
                job.Status.Result,
                job.Metadata.CreationTimestamp,
                job.Spec.Schedule,
                job.Status.StartedAt,
                job.Status.FinishedAt,
                job.Status.ExecutionStatus,
                job.Status.Reason
            );
    }
}
