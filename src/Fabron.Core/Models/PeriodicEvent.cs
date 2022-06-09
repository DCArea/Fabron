
using System;
using Fabron.Core.CloudEvents;
using Orleans;

namespace Fabron.Models;

[GenerateSerializer]
public class PeriodicEvent
{
    [Id(0)]
    public ScheduleMetadata Metadata { get; set; } = default!;

    [Id(1)]
    public PeriodicEventSpec Spec { get; set; } = default!;
};

[GenerateSerializer]
public class PeriodicEventSpec
{

    [Id(0)]
    public string Template { get; init; } = default!;

    [Id(1)]
    public TimeSpan Period { get; init; } = default!;

    [Id(2)]
    public DateTimeOffset? NotBefore { get; set; }

    [Id(3)]
    public DateTimeOffset? ExpirationTime { get; set; }

    [Id(4)]
    public bool Suspend { get; set; }
}

public record PeriodicEvent<TData>(
    ScheduleMetadata Metadata,
    PeriodicEventSpec<TData> Spec
);

public record PeriodicEventSpec<TData>(
    CloudEventTemplate<TData> Template,
    TimeSpan Period,
    DateTimeOffset? NotBefore,
    DateTimeOffset? ExpirationTime,
    bool Suspend
);

