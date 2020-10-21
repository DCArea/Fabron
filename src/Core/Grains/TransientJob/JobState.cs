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
#pragma warning disable CS8618 // need this for serialization
        public TransientJobState() { }
#pragma warning restore CS8618

        public TransientJobState(string commandName, string commandRawData)
        {
            Command = new JobCommand(commandName, commandRawData);
            Status = JobStatus.Created;
        }

        public JobCommand Command { get; }
        public JobStatus Status { get; private set; }
        public string? Reason { get; private set; }

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
