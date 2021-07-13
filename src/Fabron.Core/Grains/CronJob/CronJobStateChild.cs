// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Fabron.Grains.CronJob
{
    public class CronJobStateChild
    {
        public CronJobStateChild(DateTime scheduledAt)
        {
            Id = Guid.NewGuid().ToString();
            ScheduledAt = scheduledAt;
            Status = JobStatus.NotCreated;
        }

        public string Id { get; }
        public JobStatus Status { get; set; }
        public DateTime ScheduledAt { get; set; }

        public bool IsPending
            => Status is (JobStatus.Created or JobStatus.Running);
        public bool IsFinished
            => Status is (JobStatus.RanToCompletion or JobStatus.Canceled or JobStatus.Faulted);
    }
}
