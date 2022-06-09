
using System;
using Fabron.Core.CloudEvents;
using Orleans;

namespace Fabron.Models;

[GenerateSerializer]
public class TimedEvent
{
    [Id(0)]
    public ScheduleMetadata Metadata { get; set; } = default!;

    [Id(1)]
    public TimedEventSpec Spec { get; set; } = default!;
};

[GenerateSerializer]
public class TimedEventSpec
{
    [Id(0)]
    public string Template { get; init; } = default!;

    [Id(1)]
    public DateTimeOffset Schedule { get; init; } = default!;
}

public record TimedEvent<TData>(
    ScheduleMetadata Metadata,
    TimedEventSpec<TData> Spec
);

public record TimedEventSpec<TData>(
    DateTimeOffset Schedule,
    CloudEventTemplate<TData> Template
);
