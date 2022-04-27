
using System;
using System.Collections.Generic;
using Orleans;
using static Fabron.FabronConstants;

namespace Fabron.Models
{
    [GenerateSerializer]
    public record JobItem(
        [property: Id(0)]
        uint Index,
        [property: Id(1)]
        string Key,
        [property: Id(2)]
        DateTime Schedule,
        [property: Id(3)]
        ExecutionStatus Status
    );

    [GenerateSerializer]
    public record CronJobMetadata(
        [property: Id(0)]
        string Key,
        [property: Id(1)]
        string Uid,
        [property: Id(2)]
        DateTime CreationTimestamp,
        [property: Id(3)]
        Dictionary<string, string> Labels,
        [property: Id(4)]
        Dictionary<string, string> Annotations
    );

    [GenerateSerializer]
    public record CronJobSpec(
        [property: Id(0)]
        string Schedule,
        [property: Id(1)]
        string CommandName,
        [property: Id(2)]
        string CommandData,
        [property: Id(3)]
        DateTime? NotBefore,
        [property: Id(4)]
        DateTime? ExpirationTime,
        [property: Id(5)]
        bool Suspend);

    [GenerateSerializer]
    public record CronJobStatus(
        [property: Id(0)]
        List<JobItem> Jobs,
        [property: Id(1)]
        uint LatestScheduleIndex,
        [property: Id(2)]
        DateTime? CompletionTimestamp,
        [property: Id(3)]
        string? Reason,
        [property: Id(4)]
        bool Deleted)
    {
        public static CronJobStatus Initial
            => new(
                new List<JobItem>(),
                0,
                null,
                null,
                false);
    };

    [GenerateSerializer]
    public record CronJob(
        [property: Id(0)]
        CronJobMetadata Metadata,
        [property: Id(1)]
        CronJobSpec Spec,
        [property: Id(2)]
        CronJobStatus Status,
        [property: Id(3)]
        long Version)
    {
        public string GetChildJobKeyByIndex(DateTime schedule) => string.Format(CronItemKeyTemplate, this.Metadata.Uid, schedule.ToString("yyyy-MM-ddTHH:mm:ssZ"));
    }
}
