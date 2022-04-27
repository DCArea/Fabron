
using System;
using System.Collections.Generic;
using Orleans;

namespace Fabron.Models
{
    [GenerateSerializer]
    public record Job(
        [property: Id(0)]
        JobMetadata Metadata,
        [property: Id(1)]
        JobSpec Spec,
        [property: Id(2)]
        JobStatus Status,
        [property: Id(3)]
        long Version)
    {
        public TimeSpan DueTime
        {
            get
            {
                DateTime utcNow = DateTime.UtcNow;
                return Spec.Schedule <= utcNow ? TimeSpan.Zero : Spec.Schedule - utcNow;
            }
        }

        public TimeSpan Tardiness
            => Status.StartedAt is null || Status.StartedAt < Spec.Schedule ? TimeSpan.Zero : Status.StartedAt.Value.Subtract(Spec.Schedule);

    }

    [GenerateSerializer]
    public record JobMetadata(
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
    public record JobSpec(
        [property: Id(0)]
        DateTime Schedule,
        [property: Id(1)]
        string CommandName,
        [property: Id(2)]
        string CommandData
    );

    [GenerateSerializer]
    public record JobStatus(
        [property: Id(0)]
        ExecutionStatus ExecutionStatus,
        [property: Id(1)]
        DateTime? StartedAt,
        [property: Id(2)]
        DateTime? FinishedAt,
        [property: Id(3)]
        string? Result,
        [property: Id(4)]
        string? Reason,
        [property: Id(5)]
        bool Deleted)
    {
        public static JobStatus Initial
            => new(
                ExecutionStatus.Scheduled,
                null,
                null,
                null,
                null,
                false);
    };
}
