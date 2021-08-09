// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Fabron.Grains.Job
{


    public class JobState
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public JobState() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public DateTime CreatedAt { get; set; }
        public JobSpec Spec { get; init; }
        public JobStatus Status { get; set; }
        public TimeSpan DueTime
        {
            get
            {
                DateTime utcNow = DateTime.UtcNow;
                return Spec.Schedule < utcNow ? TimeSpan.Zero : Spec.Schedule - utcNow;
            }
        }

        public TimeSpan Tardiness
            => Status.StartedAt is null || Status.StartedAt < Spec.Schedule ? TimeSpan.Zero : Status.StartedAt.Value.Subtract(Spec.Schedule);

    }

    //public class JobSpec
    //{
    //    public DateTime Schedule { get; init; }
    //    public string CommandName { get; init; } = default!;
    //    public string CommandData { get; init; } = default!;
    //}
    public record JobSpec(
        DateTime Schedule,
        string CommandName,
        string CommandData
    );
    public record JobStatus(
        ExecutionStatus ExecutionStatus = ExecutionStatus.Created,
        DateTime? StartedAt = null,
        DateTime? FinishedAt = null,
        string? Result = null,
        string? Reason = null,
        bool Finalized = false
    );

}
