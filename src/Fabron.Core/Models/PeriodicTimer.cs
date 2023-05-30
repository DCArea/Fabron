namespace Fabron.Models;

[GenerateSerializer]
[Immutable]
public record PeriodicTimer(
    ScheduleMetadata Metadata,
    PeriodicTimerSpec Spec,
    string? Data
) : DistributedTimer<PeriodicTimerSpec>(Metadata, Spec, Data, new());

[GenerateSerializer]
[Immutable]
public record PeriodicTimerSpec
(
    [property: Id(1)]
    TimeSpan Period,

    [property: Id(2)]
    DateTimeOffset? NotBefore = null,

    [property: Id(3)]
    DateTimeOffset? ExpirationTime = null
)
: ISchedulerSpec;
