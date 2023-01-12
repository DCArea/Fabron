namespace Fabron.Models;

[GenerateSerializer]
[Immutable]
public class ScheduleMetadata
{
    [Id(0)]
    public string Key { get; set; } = default!;

    [Id(1)]
    public DateTimeOffset CreationTimestamp { get; set; }

    [Id(2)]
    public DateTimeOffset? DeletionTimestamp { get; set; }

    [Id(3)]
    public string? Owner { get; set; }

    [Id(4)]
    public Dictionary<string, string> Extensions { get; set; } = new();
}

public interface IScheduledEvent
{
    ScheduleMetadata Metadata { get; }
    string Data { get; }
}

public interface ISchedulerSpec
{
}

[GenerateSerializer]
[Immutable]
public class ScheduledEvent<TScheduleSpec> : IScheduledEvent
    where TScheduleSpec : ISchedulerSpec
{
    [Id(0)]
    public ScheduleMetadata Metadata { get; set; } = default!;

    [Id(1)]
    public string Data { get; init; } = default!;

    [Id(2)]
    public TScheduleSpec Spec { get; set; } = default!;
}
