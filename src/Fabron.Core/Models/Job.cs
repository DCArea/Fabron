
using System;
using System.Collections.Generic;

namespace Fabron.Models
{
    public class Job
    {
        public JobMetadata Metadata { get; set; } = default!;
        public JobSpec Spec { get; init; } = default!;
        public JobStatus Status { get; set; } = default!;
        public ulong Version { get; set; }

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
        string Uid,
        DateTime CreationTimestamp,
        Dictionary<string, string> Labels
    );

    public record JobSpec(
        DateTime Schedule,
        string CommandName,
        string CommandData
    );

    public record JobStatus(
        ExecutionStatus ExecutionStatus = ExecutionStatus.Scheduled,
        DateTime? StartedAt = null,
        DateTime? FinishedAt = null,
        string? Result = null,
        string? Reason = null,
        bool Finalized = false,
        ulong StateVersion = 0
    );
}
