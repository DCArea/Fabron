using System;
using System.Collections.Generic;

namespace TGH.Contracts
{
    public record CronJobBase
    (
        string CronExp,
        IEnumerable<CronChildJob> NotCreatedJobs,
        IEnumerable<CronChildJob> PendingJobs,
        IEnumerable<CronChildJob> FinishedJobs,
        JobStatus Status,
        string? Reason
    );
    public record CronJob
    (
        string CronExp,
        object Command,
        IEnumerable<CronChildJob> NotCreatedJobs,
        IEnumerable<CronChildJob> PendingJobs,
        IEnumerable<CronChildJob> FinishedJobs,
        JobStatus Status,
        string? Reason
    );

    public record CronJobDetail
    (
        string CronExp,
        object Command,
        IEnumerable<CronChildJobDetail> NotCreatedJobs,
        IEnumerable<CronChildJobDetail> PendingJobs,
        IEnumerable<CronChildJobDetail> FinishedJobs,
        JobStatus Status,
        string? Reason
    );

    public record CronChildJob
    (
        Guid JobId,
        JobStatus Status,
        DateTime ScheduledAt
    );

    public record CronChildJobDetail
    (
        Guid JobId,
        object? Result,
        JobStatus Status,
        DateTime? CreatedAt,
        DateTime? ScheduledAt,
        DateTime? StartedAt,
        DateTime? FinishedAt
    );

    public static class CronJobExtensions
    {
        public static bool IsPending(this CronChildJobDetail job)
            => job.Status is JobStatus.Created or JobStatus.Running;

        public static bool IsFinished(this CronChildJobDetail job)
            => job.Status is JobStatus.RanToCompletion or JobStatus.Canceled or JobStatus.Faulted;
    }
}
