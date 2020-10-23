using System;

namespace TGH.Grains.TransientJob
{

    public class JobCommand
    {
        public JobCommand(string name, string data)
        {
            Name = name;
            Data = data;
        }

        public string Name { get; }
        public string Data { get; }
        public string? Result { get; set; }
    }

    public class TransientJobState
    {
#nullable disable
        public TransientJobState() { }
#nullable enable

        public TransientJobState(JobCommandInfo command, DateTime? scheduledAt)
        {
            Command = new JobCommand(command.Name, command.Data);
            CreatedAt = DateTime.UtcNow;
            ScheduledAt = scheduledAt;
            Status = JobStatus.Created;
        }

        public JobCommand Command { get; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? ScheduledAt { get; private set; }
        public DateTime? StartedAt { get; private set; }
        public DateTime? FinishedAt { get; private set; }
        public JobStatus Status { get; private set; }
        public string? Reason { get; private set; }

        public TimeSpan DueTime
        {
            get
            {
                DateTime utcNow = DateTime.UtcNow;
                if (ScheduledAt is null || ScheduledAt.Value < utcNow)
                    return TimeSpan.Zero;
                var scheduledAt = ScheduledAt.Value;
                TimeSpan dueTime = scheduledAt - DateTime.UtcNow;
                if (dueTime < TimeSpan.FromSeconds(10))
                    dueTime = TimeSpan.Zero;
                return dueTime;
            }
        }

        public void Start()
        {
            StartedAt = DateTime.UtcNow;
            Status = JobStatus.Running;
        }

        public void Complete(string? result)
        {
            Status = JobStatus.RanToCompletion;
            FinishedAt = DateTime.UtcNow;
            Command.Result = result;
        }

        public void Cancel(string reason)
        {
            Status = JobStatus.Canceled;
            FinishedAt = DateTime.UtcNow;
            Reason = reason;
        }

        public void Fault(Exception exception)
        {
            Status = JobStatus.Faulted;
            FinishedAt = DateTime.UtcNow;
            Reason = exception.ToString();
        }
    }
}
