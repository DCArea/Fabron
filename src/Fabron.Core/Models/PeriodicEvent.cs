namespace Fabron.Models;

[GenerateSerializer]
[Immutable]
public class PeriodicEvent : ScheduledEvent<PeriodicEventSpec>
{ }

[GenerateSerializer]
[Immutable]
public class PeriodicEventSpec : ISchedulerSpec
{

    [Id(1)]
    public TimeSpan Period { get; init; }

    [Id(2)]
    public DateTimeOffset? NotBefore { get; init; }

    [Id(3)]
    public DateTimeOffset? ExpirationTime { get; init; }

    [Id(4)]
    public bool Suspend { get; init; }
}

public record PeriodicEvent<TData>(
    ScheduleMetadata Metadata,
    TData Data,
    PeriodicEventSpec Spec
);

