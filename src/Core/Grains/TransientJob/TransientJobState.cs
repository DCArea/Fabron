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
            ScheduledAt = scheduledAt;
            Status = JobStatus.Created;
        }

        public JobCommand Command { get; }
        public DateTime? ScheduledAt { get; }
        public JobStatus Status { get; private set; }
        public string? Reason { get; private set; }

        public TimeSpan DueTime
        {
            get
            {
                if (ScheduledAt is null)
                    return TimeSpan.Zero;
                var scheduledAt = ScheduledAt.Value;
                TimeSpan dueTime = scheduledAt - DateTime.UtcNow;
                if (dueTime < TimeSpan.FromSeconds(10))
                    dueTime = TimeSpan.Zero;
                return dueTime;
            }
        }

        public void Start() => Status = JobStatus.Running;

        public void Complete(string? result)
        {
            Status = JobStatus.RanToCompletion;
            Command.Result = result;
        }

        public void Cancel(string reason)
        {
            Status = JobStatus.Canceled;
            Reason = reason;
        }

        public void Fault(Exception exception)
        {
            Status = JobStatus.Faulted;
            Reason = exception.ToString();
        }
    }
}
