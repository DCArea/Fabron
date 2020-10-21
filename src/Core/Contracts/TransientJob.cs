namespace TGH.Contracts
{
    public record JobCommand<TCommand, TResult>(
        TCommand Data,
        TResult? Result
    ) where TCommand : ICommand<TResult>;

    public record JobCommand(
        object Data,
        object? Result
    );

    public record JobCommandRaw(
        string Name,
        string Data,
        string Result
    ) : JobCommandRawInfo(Name, Data);

    public record TransientJob<TCommand, TResult>
    (
        JobCommand<TCommand, TResult> Command,
        JobStatus Status,
        string? Reason
    ) : JobInfo(Status, Reason)
        where TCommand : ICommand<TResult>;

}
