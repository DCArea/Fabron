using System;

namespace TGH.Grains.CronJob
{
    public class ChildJobState
    {
        public ChildJobState(DateTime scheduledAt)
        {
            Id = Guid.NewGuid();
            ScheduledAt = scheduledAt;
            Status = JobStatus.NotCreated;
        }

        public Guid Id { get; }
        public JobStatus Status { get; set; }
        public DateTime ScheduledAt { get; set; }
        public bool IsFinished => Status switch
        {
            JobStatus.RanToCompletion or JobStatus.Faulted or JobStatus.Canceled => true,
            _ => false
        };
    }
}
