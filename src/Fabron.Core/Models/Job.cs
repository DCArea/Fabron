
using System;
using System.Collections.Generic;

namespace Fabron.Models
{
    public record Job(
        JobMetadata Metadata,
        JobSpec Spec,
        JobStatus Status,
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

    public record JobMetadata(
        string Key,
        string Uid,
        DateTime CreationTimestamp,
        Dictionary<string, string> Labels,
        Dictionary<string, string> Annotations
    );

    public record JobSpec(
        DateTime Schedule,
        string CommandName,
        string CommandData
    );

    public record JobStatus(
        ExecutionStatus ExecutionStatus,
        DateTime? StartedAt,
        DateTime? FinishedAt,
        string? Result,
        string? Reason,
        bool Deleted)
    {
        public static JobStatus Initial
            => new JobStatus(
                ExecutionStatus.Scheduled,
                null,
                null,
                null,
                null,
                false);
    };
}
