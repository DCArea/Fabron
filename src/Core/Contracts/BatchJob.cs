using System;
using System.Collections.Generic;

namespace TGH.Contracts
{
    public record BatchChildJob
    (
        Guid JobId,
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
