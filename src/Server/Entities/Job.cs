using System;
using System.Text.Json;
using TGH.Server.Grains;

namespace TGH.Server.Entities
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
}
