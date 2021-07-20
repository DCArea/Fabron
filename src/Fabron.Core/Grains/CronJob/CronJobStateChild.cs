// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Fabron.Grains.CronJob
{
    public enum CronChildJobStatus
    {
        WaitToSchedule,
        Scheduled,
        RanToCompletion,
        Canceled,
        Faulted
    }

    public class CronJobStateChild
    {
        public CronJobStateChild(DateTime scheduledAt)
        {
            Id = Guid.NewGuid().ToString();
            ScheduledAt = scheduledAt;
            Status = CronChildJobStatus.WaitToSchedule;
        }

        public string Id { get; }
        public CronChildJobStatus Status { get; set; }
        public DateTime ScheduledAt { get; set; }

        public bool IsFinished
            => Status is (CronChildJobStatus.RanToCompletion or CronChildJobStatus.Canceled or CronChildJobStatus.Faulted);
    }
}
