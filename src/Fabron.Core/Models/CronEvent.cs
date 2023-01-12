namespace Fabron.Models;

[GenerateSerializer]
[Immutable]
public class CronEvent : ScheduledEvent<CronEventSpec>
{ };

[GenerateSerializer]
[Immutable]
public class CronEventSpec : ISchedulerSpec
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
    TData Data,
    CronEventSpec Spec
);

