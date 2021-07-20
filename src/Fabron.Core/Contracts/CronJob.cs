// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Fabron.Contracts
{
    public record CronJobBase
    (
        string CronExp,
        IEnumerable<CronChildJob> PendingJobs,
        IEnumerable<CronChildJob> ScheduledJobs,
        IEnumerable<CronChildJob> FinishedJobs,
        JobStatus Status,
        string? Reason
    );
    public record CronJob
    (
        string CronExp,
        object Command,
        IEnumerable<CronChildJob> PendingJobs,
        IEnumerable<CronChildJob> ScheduledJobs,
        IEnumerable<CronChildJob> FinishedJobs,
        JobStatus Status,
        string? Reason
    );

    public record CronJobDetail
    (
        string CronExp,
        object Command,
        IEnumerable<CronChildJobDetail> PendingJobs,
        IEnumerable<CronChildJobDetail> ScheduledJobs,
        IEnumerable<CronChildJobDetail> FinishedJobs,
        JobStatus Status,
        string? Reason
    );

    public record CronChildJob
    (
        string JobId,
        JobStatus Status,
        DateTime ScheduledAt
    );

    public record CronChildJobDetail
    (
        string JobId,
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
