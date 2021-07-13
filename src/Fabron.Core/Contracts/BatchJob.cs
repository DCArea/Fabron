// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Fabron.Contracts
{
    public record BatchChildJob
    (
        string JobId,
        object Command
    );
    public record BatchJob
    (
        IEnumerable<BatchChildJob> PendingJobs,
        IEnumerable<BatchChildJob> FinishedJobs,
        JobStatus Status,
        string? Reason
    ) : JobInfo(Status, Reason);

}
