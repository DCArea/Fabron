using System;
using System.Collections.Generic;
using System.Linq;

namespace TGH.Server.Grains.BatchJob
{
    public class ChildJobCommand
    {
#nullable disable
        public ChildJobCommand() { }
#nullable enable

        public ChildJobCommand(string name, string data)
        {
            Name = name;
            Data = data;
        }

        public string Name { get; }
        public string Data { get; }
    }

    public class ChildJobState
    {
#nullable disable
        public ChildJobState() { }
#nullable enable
        public ChildJobState(ChildJobCommand command)
        {
            Command = command;
            Id = Guid.NewGuid();
        }

        public Guid Id { get; }
        public ChildJobCommand Command { get; }
        public JobStatus Status { get; set; }
        public bool IsFinished => Status switch
        {
            (JobStatus.RanToCompletion or JobStatus.Faulted or JobStatus.Canceled) => true,
            _ => false
        };
    }

    public class BatchJobState
    {
#nullable disable
        public BatchJobState() { }
#nullable enable
        public BatchJobState(List<ChildJobState> childJobs)
        {
            _childJobs = childJobs;
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
