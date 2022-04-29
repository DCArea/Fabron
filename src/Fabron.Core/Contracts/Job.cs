using System;
using Fabron.Mando;
using Fabron.Models;

namespace Fabron.Contracts;

public record Job<TCommand, TResult>
(
    ObjectMetadata Metadata,
    JobSpec<TCommand> Spec,
    JobStatus<TResult> Status
) where TCommand : ICommand<TResult>;

public record CommandSpec<TCommand>(
    string Name,
    TCommand Data
) where TCommand : ICommand;

public record JobSpec<TCommand>(
    CommandSpec<TCommand> Command,
    DateTimeOffset? Schedule
) where TCommand : ICommand;

public record JobStatus<TResult>(
    JobExecutionStatus ExecutionStatus = JobExecutionStatus.Scheduled,
    TResult? Result = default,
    string? Reason = null,
    string? Message = null
);
