
using System;
using System.Collections.Generic;
using System.Linq;
using static Fabron.FabronConstants;

namespace Fabron.Models
{
    public record JobItem(
        uint Index,
        string Key,
        DateTime Schedule,
        ExecutionStatus Status
    );

    public record CronJobMetadata(
        string Key,
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
        uint LatestScheduleIndex,
        DateTime? CompletionTimestamp,
        string? Reason,
        bool Deleted)
    {
        public static CronJobStatus Initial
            => new CronJobStatus(
                new List<JobItem>(),
                0,
                null,
                null,
                false);
    };

    public record CronJob(
        CronJobMetadata Metadata,
        CronJobSpec Spec,
        CronJobStatus Status,
        long Version)
    {
        public string GetChildJobKeyByIndex(DateTime schedule) => string.Format(CronItemKeyTemplate, this.Metadata.Uid, schedule.ToString("yyyy-MM-ddTHH:mm:ssZ"));
    }
}
