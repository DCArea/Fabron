namespace Fabron.Models;

[GenerateSerializer]
[Immutable]
public class PeriodicTimer : DistributedTimer<PeriodicTimerSpec>
{ }

[GenerateSerializer]
[Immutable]
public class PeriodicTimerSpec : ISchedulerSpec
{
    [Id(1)]
    public TimeSpan Period { get; init; }

    [Id(2)]
    public DateTimeOffset? NotBefore { get; init; }

    [Id(3)]
    public DateTimeOffset? ExpirationTime { get; init; }
}

