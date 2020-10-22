namespace TGH.Contracts
{
    public record CronJob<TCommand, TResult>
    (
        string CronExp,
        JobCommand<TCommand, TResult> Command,
        JobStatus Status,
        string? Reason
    ) where TCommand : ICommand<TResult>;

}
