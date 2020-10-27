using System;
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
