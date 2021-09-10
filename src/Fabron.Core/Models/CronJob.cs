
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fabron.Models
{
    public record JobItem(
        uint Index,
        string Uid,
        DateTime Schedule,
        ExecutionStatus Status
    );

    public record CronJobMetadata(
        string Uid,
        DateTime CreationTimestamp,
        Dictionary<string, string> Labels,
        Dictionary<string, string> Annotations
    );

    public record CronJobSpec(
        string Schedule,
        string CommandName,
        string CommandData,
        DateTime? NotBefore,
        DateTime? ExpirationTime,
        bool Suspend);

    public record CronJobStatus(
        List<JobItem> Jobs,
        uint LatestScheduleIndex = 0,
        DateTime? CompletionTimestamp = null,
        string? Reason = null)
    {
        public static CronJobStatus Initial
            => new CronJobStatus(
                new List<JobItem>(),
                0,
                null,
                null);
    };

    public record CronJob(
        CronJobMetadata Metadata,
        CronJobSpec Spec,
        CronJobStatus Status,
        long Version)
    {

        public IEnumerable<JobItem> RunningJobs => Status.Jobs.Where(item => item.Status == ExecutionStatus.Scheduled);
        public IEnumerable<JobItem> FinishedJobs => Status.Jobs.Where(item => item.Status is ExecutionStatus.Succeed or ExecutionStatus.Faulted);

        public JobItem? LatestItem => Status.Jobs.LastOrDefault();

        public bool HasRunningJobs => Status.Jobs.Any(item => item.Status == ExecutionStatus.Scheduled);

        public DateTime? GetNextTick(DateTime notBefore)
        {
            Cronos.CronExpression cron = Cronos.CronExpression.Parse(Spec.Schedule);
            JobItem? lastedJob = LatestItem;
            DateTime fromUtc = Spec.NotBefore ?? Metadata.CreationTimestamp;
            fromUtc = lastedJob is null ? fromUtc : lastedJob.Schedule;
            fromUtc = fromUtc > notBefore ? fromUtc : notBefore;
            DateTime? nextTick = cron.GetNextOccurrence(fromUtc, true);
            if (nextTick is null || nextTick.Value > Spec.ExpirationTime)
            {
                return null;
            }
            return nextTick;
        }

        public string GetChildJobIdByIndex(uint index) => Metadata.Uid + "-" + index;
    }
}
