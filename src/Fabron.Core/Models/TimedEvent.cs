namespace Fabron.Models;

[GenerateSerializer]
[Immutable]
public class TimedEvent : ScheduledEvent<TimedEventSpec>
{ }

[GenerateSerializer]
[Immutable]
public class TimedEventSpec : ISchedulerSpec
{
    [Id(0)]
    public DateTimeOffset Schedule { get; init; } = default!;
}

