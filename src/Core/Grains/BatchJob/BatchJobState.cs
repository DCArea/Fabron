using System;
using System.Collections.Generic;
using System.Linq;

namespace TGH.Grains.BatchJob
{
    public class ChildJobState
    {
#nullable disable
        public ChildJobState() { }
#nullable enable
        public ChildJobState(JobCommandInfo command)
        {
            Command = command;
            Id = Guid.NewGuid();
        }

        public Guid Id { get; }
        public JobCommandInfo Command { get; }
        public JobStatus Status { get; set; }
        public bool IsFinished => Status switch
        {
            JobStatus.RanToCompletion or JobStatus.Faulted or JobStatus.Canceled => true,
            _ => false
        };
    }

    public class BatchJobState
    {
#nullable disable
        public BatchJobState() { }
#nullable enable
        public BatchJobState(List<JobCommandInfo> commands)
        {
            _childJobs = commands
                .Select(cmd => new ChildJobState(cmd))
                .ToList();
        }

        private readonly List<ChildJobState> _childJobs = null!;
        public IEnumerable<ChildJobState> NotStartedJobs => _childJobs.Where(job => job.Status == JobStatus.NotCreated || job.Status == JobStatus.Created);
        public IEnumerable<ChildJobState> RunningJobs => _childJobs.Where(job => job.Status == JobStatus.Running);
        public IEnumerable<ChildJobState> FinishedJobs => _childJobs.Where(job => job.IsFinished);
        public IEnumerable<ChildJobState> PendingJobs => _childJobs.Where(job => !job.IsFinished);

        public string? Reason { get; private set; }

        public JobStatus Status { get; private set; }

        public void Start()
        {
            Status = JobStatus.Running;
        }

        public void Complete()
        {
            Status = JobStatus.RanToCompletion;
        }

        public void Cancel(string reason)
        {
            Status = JobStatus.Canceled;
            Reason = reason;
        }

    }
}
