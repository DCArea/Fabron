// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Fabron.Mando;
using Fabron.Models;

namespace Fabron.Contracts
{
    public record TypedJobSpec<TCommand>(
        DateTime Schedule,
        string CommandName,
        TCommand CommandData
    ) where TCommand : ICommand;

    public record TypedJobStatus<TResult>(
        ExecutionStatus ExecutionStatus,
        DateTime? StartedAt,
        DateTime? FinishedAt,
        TResult? Result,
        string? Reason,
        bool Finalized
    );

    public record Job<TCommand, TResult>
    (
        JobMetadata Metadata,
        TypedJobSpec<TCommand> Spec,
        TypedJobStatus<TResult> Status
    ) where TCommand : ICommand<TResult>;
}
