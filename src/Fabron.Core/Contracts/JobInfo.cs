// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        Scheduled,
        Started,
        Succeed,
        Canceled,
        Faulted
    }

}
