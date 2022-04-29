using System;
using Orleans;

namespace Fabron.Models;

[GenerateSerializer]
public class Job
{
    [Id(0)]
    public ObjectMetadata Metadata { get; set; } = default!;

    [Id(1)]
    public JobSpec Spec { get; set; } = default!;

    [Id(2)]
    public JobStatus Status { get; set; } = default!;

    public bool Deleted => Metadata.DeletionTimestamp.HasValue;
}

[GenerateSerializer]
public class JobSpec
{
    [Id(0)]
    public CommandSpec Command { get; set; } = default!;

    [Id(1)]
    public DateTimeOffset? Schedule { get; set; }
};

[GenerateSerializer]
public class JobStatus
{

    [Id(0)]
    public JobExecutionStatus ExecutionStatus { get; set; }

    [Id(1)]
    public string? Result { get; set; }

    [Id(2)]
    public string? Reason { get; set; }

    [Id(3)]
    public string? Message { get; set; }
}

[GenerateSerializer]
public enum JobExecutionStatus
{
    [Id(0)]
    Scheduled,

    [Id(1)]
    Started,

    [Id(2)]
    Complete,

    [Id(3)]
    Failed,
}
