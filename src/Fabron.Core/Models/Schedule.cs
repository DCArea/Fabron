namespace Fabron.Models;

[GenerateSerializer]
[Immutable]
public record ScheduleMetadata
(
    [property: Id(0)]
    string Key,

    [property: Id(1)]
    DateTimeOffset CreationTimestamp,

    [property: Id(2)]
    DateTimeOffset? DeletionTimestamp,

    [property: Id(3)]
    string? Owner,

    [property: Id(4)]
    Dictionary<string, string> Extensions
);

public interface IDistributedTimer
{
    ScheduleMetadata Metadata { get; }
    string? Data { get; }
    TickerStatus Status { get; }
}

public interface ISchedulerSpec
{
}

[GenerateSerializer]
[Immutable]
public record DistributedTimer<TScheduleSpec>
(
    [property:Id(0)]
    ScheduleMetadata Metadata,

    [property:Id(1)]
    TScheduleSpec Spec,

    [property:Id(2)]
    string? Data,

    [property:Id(3)]
    TickerStatus Status
) : IDistributedTimer where TScheduleSpec : ISchedulerSpec;

//[GenerateSerializer]
//[Immutable]
//public record TickerStatus
//(
//    [property: Id(0)]
//    string Key,

//    [property: Id(1)]
//    DateTimeOffset CreationTimestamp,

//    [property: Id(2)]
//    DateTimeOffset? DeletionTimestamp,

//    [property: Id(3)]
//    string? Owner,

//    [property: Id(4)]
//    Dictionary<string, string> Extensions
//);
