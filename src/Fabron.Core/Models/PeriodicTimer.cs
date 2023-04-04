namespace Fabron.Models;

[GenerateSerializer]
[Immutable]
public record PeriodicTimer(
    ScheduleMetadata Metadata,
    PeriodicTimerSpec Spec,
    string? Data
) : DistributedTimer<PeriodicTimerSpec>(Metadata, Spec, Data);

[GenerateSerializer]
[Immutable]
public record PeriodicTimerSpec
(
    [property: Id(1)]
    TimeSpan Period,

    [property: Id(2)]
    DateTimeOffset? NotBefore,

    [property: Id(3)]
    DateTimeOffset? ExpirationTime
)
: ISchedulerSpec;
