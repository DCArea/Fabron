using System;
using System.Collections.Generic;

namespace TGH.Contracts
{
    public record ChildJob
    (
        Guid JobId,
        object Command
    );
    public record BatchJob
    (
        IEnumerable<ChildJob> PendingJobs,
        IEnumerable<ChildJob> FinishedJobs,
        JobStatus Status,
        string? Reason
    ) : JobInfo(Status, Reason);

}
