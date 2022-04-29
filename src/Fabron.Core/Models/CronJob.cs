using System;
using Orleans;

namespace Fabron.Models;

[GenerateSerializer]
public class CronJob
{
    [Id(0)]
    public ObjectMetadata Metadata { get; set; } = default!;

    [Id(1)]
    public CronJobSpec Spec { get; set; } = default!;

    [Id(2)]
    public CronJobStatus Status { get; set; } = default!;

    public bool Deleted => Metadata.DeletionTimestamp.HasValue;
}

[GenerateSerializer]
public class CronJobSpec
{
    [property: Id(0)]
    public CommandSpec Command { get; set; } = default!;

    [property: Id(1)]
    public string Schedule { get; set; } = default!;

    [property: Id(2)]
    public DateTimeOffset? NotBefore { get; set; }

    [property: Id(3)]
    public DateTimeOffset? ExpirationTime { get; set; }

    [property: Id(4)]
    public bool Suspend { get; set; }
}

[GenerateSerializer]
public class CronJobStatus
{
    [property: Id(0)]
    public DateTimeOffset? LastScheduleTime { get; set; }
}
