using TGH.Grains.TransientJob;

namespace TGH.Contracts
{
    public record JobInfo
    (
        JobStatus Status,
        string? Reason
    );

    public record Job<TCommand, TResult>
    (
        string CommandName,
        TCommand CommandData,
        TResult? Result,
        JobStatus Status,
        string? Reason
    ) : JobInfo(Status, Reason) where TCommand : ICommand<TResult>;

    public record JobCommand(
        string CommandName,
        ICommand CommandData,
        object? Result
    );
    public record JobCommand<TCommand, TResult>(
        string CommandName,
        object CommandData,
        TResult? Result
    ) where TCommand : ICommand<TResult>;
    public record BatchJob
    (
        // List<JobCommand> Commands,
        // string CommandName,
        // TCommand CommandData,
        // TResult? Result,
        JobStatus Status,
        string? Reason
    ) : JobInfo(Status, Reason);

    public static class JobExtensions
    {
        // public static BatchJob From(this BatchJobState jobState)
        // {
        //     var commands = jobState.RunningJobs()
        //     JsonSerializer.Deserialize(jobState.)
        //     return new BatchJob()

        // }
    }

}
