namespace TGH.Contracts
{
    public record JobCommandInfo<TCommand, TResult>(
        TCommand CommandData
    ) where TCommand : ICommand<TResult>;

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
