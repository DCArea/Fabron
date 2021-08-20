// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Fabron.Grains;
using Fabron.Mando;

namespace Fabron.Contracts
{
    public record CronJobBase
    (
        string Schedule,
        IEnumerable<CronChildJob> PendingJobs,
        IEnumerable<CronChildJob> ScheduledJobs,
        IEnumerable<CronChildJob> FinishedJobs,
        JobStatus Status,
        string? Reason
    );

    public record CronJob<TCommand>
    (
        string Schedule,
        TCommand Command,
        IEnumerable<CronChildJob> ScheduledJobs,
        string? Reason
    ) where TCommand : ICommand;

    public record CronJob
    (
        string Schedule,
        ICommand Command,
        IEnumerable<CronChildJob> ScheduledJobs,
        string? Reason
    );

    public record CronJobDetail
    (
        string Schedule,
        object Command,
        IEnumerable<CronChildJobDetail> ScheduledJobs,
        string? Reason
    );

    public record CronChildJob
    (
        string JobId,
        ExecutionStatus Status,
        DateTime ScheduledAt
    );

    public record CronChildJobDetail
    (
        string JobId,
        object? Result,
        ExecutionStatus Status,
        DateTime? CreatedAt,
        DateTime? ScheduledAt,
        DateTime? StartedAt,
        DateTime? FinishedAt
    );

    public static class CronJobExtensions
    {
        public static bool IsPending(this CronChildJobDetail job)
            => job.Status is ExecutionStatus.NotScheduled or ExecutionStatus.Scheduled or ExecutionStatus.Started;

        public static bool IsFinished(this CronChildJobDetail job)
            => job.Status is ExecutionStatus.Succeed or ExecutionStatus.Canceled or ExecutionStatus.Faulted;
    }
}
