using System;
using System.Text.Json;

namespace TGH.Server.Entities
{
    //public interface IJob
    //{ }

    //public interface IJob<TCommand> where TCommand : ICommand
    //{
    //    TCommand Command { get; }
    //}

    //public abstract class Job<TCommand> where TCommand : ICommand
    //{
    //    protected Job(TCommand command)
    //    {
    //        Command = command;
    //    }
    //    public TCommand Command { get; init; }
    //    public byte[] RawCommand => JsonSerializer.SerializeToUtf8Bytes(Command);
    //    public Job NormalizedJob => new Job(RawCommand);
    //}

    //public class Job
    //{
    //    private readonly byte[] rawCommand;
    //    public Job(byte[] command)
    //    {
    //        rawCommand = command;
    //    }

    //    public TCommand GetTypedCommand<TCommand>() where TCommand : ICommand
    //    {
    //        var typedCommand = JsonSerializer.Deserialize<TCommand>(rawCommand);
    //        return typedCommand!;
    //    }
    //}

}
