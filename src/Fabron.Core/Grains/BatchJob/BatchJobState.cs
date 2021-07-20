// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace Fabron.Grains.BatchJob
{
    public enum BatchJobStatus
    {
        Created,
        Running,
        RanToCompletion,
        Canceled,
    }
    public class BatchJobState
    {
#nullable disable
        public BatchJobState() { }
#nullable enable
        public BatchJobState(List<JobCommandInfo> commands) => _childJobs = commands
                .Select(cmd => new BatchJobStateChild(cmd))
                .ToList();

        private readonly List<BatchJobStateChild> _childJobs = null!;
        public IEnumerable<BatchJobStateChild> PendingJobs => _childJobs.Where(job => job.Status == ChildJobStatus.WaitToSchedule);
        public IEnumerable<BatchJobStateChild> ScheduledJobs => _childJobs.Where(job => job.Status == ChildJobStatus.Scheduled);
        public IEnumerable<BatchJobStateChild> FinishedJobs => _childJobs.Where(job => job.IsFinished);

        public string? Reason { get; private set; }

        public BatchJobStatus Status { get; private set; }

        public void Start() => Status = BatchJobStatus.Running;

        public void Complete() => Status = BatchJobStatus.RanToCompletion;

        public void Cancel(string reason)
        {
            Status = BatchJobStatus.Canceled;
            Reason = reason;
        }

    }
}
