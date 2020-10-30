using Fabron.Mando;

namespace Fabron.Contracts
{
    public record JobCommandInfo<TCommand>(
        TCommand CommandData
    ) where TCommand : ICommand;

    public record JobCommandInfo(
        object Data
    );
    public record JobCommandRawInfo(
        string CommandName,
        string CommandData
    );

    public record JobInfo
    (
        JobStatus Status,
        string? Reason
    );

    public enum JobStatus
    {
        NotCreated,
        Created,
        Running,
        RanToCompletion,
        Canceled,
        Faulted
    }

}
