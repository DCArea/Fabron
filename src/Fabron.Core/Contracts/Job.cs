
using System;
using Fabron.Mando;
using Fabron.Models;

namespace Fabron.Contracts
{
    public record JobSpec<TCommand>(
        DateTime Schedule,
        string CommandName,
        TCommand CommandData
    ) where TCommand : ICommand;

    public record JobStatus<TResult>(
        ExecutionStatus ExecutionStatus,
        DateTime? StartedAt,
        DateTime? FinishedAt,
        TResult? Result,
        string? Reason
    );

    public record Job<TCommand, TResult>
    (
        JobMetadata Metadata,
        JobSpec<TCommand> Spec,
        JobStatus<TResult> Status
    ) where TCommand : ICommand<TResult>;
}
