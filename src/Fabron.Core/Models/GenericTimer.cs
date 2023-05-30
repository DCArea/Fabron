namespace Fabron.Models;

[GenerateSerializer]
[Immutable]
public record GenericTimer(
    ScheduleMetadata Metadata,
    GenericTimerSpec Spec,
    string? Data
) : DistributedTimer<GenericTimerSpec>(Metadata, Spec, Data, new());

[GenerateSerializer]
[Immutable]
public record GenericTimerSpec(
    [property: Id(0)]
    DateTimeOffset Schedule
) : ISchedulerSpec;
