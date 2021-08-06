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

        public JobSpec Spec { get; init; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; } = null;
        public DateTime? FinishedAt { get; set; } = null;
        public string? Result { get; set; } = null;
        public string? Reason { get; set; } = null;
        public JobStatus Status { get; set; } = JobStatus.Created;
        public bool Finalized { get; set; } = false;

        public TimeSpan DueTime
        {
            get
            {
                DateTime utcNow = DateTime.UtcNow;
                return Spec.Schedule < utcNow ? TimeSpan.Zero : Spec.Schedule - utcNow;
            }
        }

        public TimeSpan Tardiness
            => StartedAt is null || StartedAt < Spec.Schedule ? TimeSpan.Zero : StartedAt.Value.Subtract(Spec.Schedule);

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

}
