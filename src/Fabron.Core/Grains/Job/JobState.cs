// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Fabron.Grains.Job
{
    public class JobState
    {
        public JobMetadata Metadata { get; set; } = default!;
        public JobSpec Spec { get; init; } = default!;
        public JobStatus Status { get; set; } = default!;

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
        Dictionary<string, string> Labels,
        long ResourceVersion = 0
    );

    public record JobSpec(
        DateTime Schedule,
        string CommandName,
        string CommandData
    );

    public record JobStatus(
        ExecutionStatus ExecutionStatus = ExecutionStatus.NotScheduled,
        DateTime? StartedAt = null,
        DateTime? FinishedAt = null,
        string? Result = null,
        string? Reason = null,
        bool Finalized = false
    );
}
