
using System;
using Fabron.CloudEvents;
using Orleans;

namespace Fabron.Models;

[GenerateSerializer]
[Immutable]
public class CronEvent : ScheduledEvent<CronEventSpec>
{ };

[GenerateSerializer]
[Immutable]
public class CronEventSpec: ISchedulerSpec
{
    [Id(0)]
    public string Schedule { get; init; } = default!;

    [Id(1)]
    public DateTimeOffset? NotBefore { get; set; }

    [Id(2)]
    public DateTimeOffset? ExpirationTime { get; set; }

    [Id(3)]
    public bool Suspend { get; set; }
}

public record CronEvent<TData>(
    ScheduleMetadata Metadata,
    CloudEventTemplate<TData> Template,
    CronEventSpec<TData> Spec
);

public record CronEventSpec<TData>(
    string Schedule,
    DateTimeOffset? NotBefore,
    DateTimeOffset? ExpirationTime,
    bool Suspend
);

