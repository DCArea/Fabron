// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Fabron.Grains.CronJob
{
    //public enum CronJobStatus
    //{
    //    Created,
    //    Running,
    //    RanToCompletion,
    //    Canceled,
    //}

    public record JobItem(
        uint Index,
        string Uid,
        DateTime Schedule,
        ExecutionStatus Status
    );

    public record CronJobMetadata(
        string Uid,
        DateTime CreationTimestamp,
        Dictionary<string, string> Labels
    );

    public record CronJobSpec(
        string Schedule,
        string CommandName,
        string CommandData,
        DateTime StartTimestamp,
        DateTime EndTimeStamp
    );

    public record CronJobStatus(
        List<JobItem> Jobs,
        uint LatestScheduleIndex = 0,
        DateTime? CompletionTimestamp = null,
        string? Reason = null,
        bool Finalized = false
    );

    public class CronJobState
    {
        public CronJobMetadata Metadata { get; init; } = default!;
        public CronJobSpec Spec { get; init; } = default!;
        public CronJobStatus Status { get; set; } = default!;

        public IEnumerable<JobItem> RunningJobs => Status.Jobs.Where(item => item.Status == ExecutionStatus.Scheduled);
        public IEnumerable<JobItem> FinishedJobs => Status.Jobs.Where(item => item.Status is ExecutionStatus.Succeed or ExecutionStatus.Faulted);

        public JobItem? LatestItem => Status.Jobs.LastOrDefault();

        public bool HasRunningJobs => Status.Jobs.Any(item => item.Status == ExecutionStatus.Scheduled);

        public DateTime? GetNextSchedule()
        {
            Cronos.CronExpression cron = Cronos.CronExpression.Parse(Spec.Schedule);
            var lastedJob = LatestItem;
            DateTime lastestScheduledAt = lastedJob is null ? Spec.StartTimestamp : lastedJob.Schedule;
            DateTime? nextSchedule = cron.GetNextOccurrence(lastestScheduledAt, true);
            if (nextSchedule is null || nextSchedule.Value > Spec.EndTimeStamp)
            {
                return null;
            }
            return nextSchedule;
        }

        public string GetChildJobIdByIndex(uint index) => Metadata.Uid + "-" + index;
    }
}
