using System;
using TGH.Server.Entities;

namespace TGH.Server.Grains
{
    public enum JobStatus
    {
        NotCreated,
        Created,
        Running,
        RanToCompletion,
        Canceled,
        Faulted
    }

    public record JobCommandInfo
    (
        string Name,
        string Data,
        string? Result
    );
    // public class JobCommandInfo
    // {
    //     public JobCommandInfo(string name, string data)
    //     {
    //         Name = name;
    //         Data = data;
    //     }

    //     public string Name { get; }
    //     public string Data { get; }
    //     public string? Result { get; }
    // }
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
#pragma warning disable CS8618 // need this for serialization
        public JobState() { }
#pragma warning restore CS8618

        public JobState(string commandName, string commandRawData)
        {
            command = new JobCommand(commandName, commandRawData);
            Status = JobStatus.Created;
        }

        private readonly JobCommand command;
        public JobCommandInfo Command => new(command.Name, command.Data, command.Result);
        public JobStatus Status { get; private set; }
        public string? Reason { get; private set; }

        public void Start()
        {
            Status = JobStatus.Running;
        }

        public void Complete(string? result)
        {
            Status = JobStatus.RanToCompletion;
            command.Result = result;
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
