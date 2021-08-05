// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Fabron.Grains.Job
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

    public class JobState
    {
#nullable disable
        public JobState() { }
#nullable enable

        public JobState(JobCommandInfo command, DateTime? scheduledAt)
        {
            Command = new JobCommand(command.Name, command.Data);
            CreatedAt = DateTime.UtcNow;
            ScheduledAt = scheduledAt is null || scheduledAt.Value < CreatedAt ? CreatedAt : (DateTime)scheduledAt;
            Status = JobStatus.Created;
        }

        public JobCommand Command { get; set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime ScheduledAt { get; set; }
        public DateTime? StartedAt { get; private set; }
        public DateTime? FinishedAt { get; private set; }
        public string? Reason { get; private set; }
        public JobStatus Status { get; set; }
        public bool Finalized { get; set; }
        public TimeSpan DueTime
        {
            get
            {
                DateTime utcNow = DateTime.UtcNow;
                return ScheduledAt < utcNow ? TimeSpan.Zero : ScheduledAt - utcNow;
            }
        }

        public TimeSpan Tardiness
            => StartedAt is null || StartedAt < ScheduledAt ? TimeSpan.Zero : StartedAt.Value.Subtract(ScheduledAt);

        public void Start()
        {
            StartedAt = DateTime.UtcNow;
            Status = JobStatus.Started;
        }

        public void Complete(string? result)
        {
            Status = JobStatus.Succeed;
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

    public record OnJobStateChanged(string eTag, JobState state);
}
