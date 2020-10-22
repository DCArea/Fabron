using System;

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
}
