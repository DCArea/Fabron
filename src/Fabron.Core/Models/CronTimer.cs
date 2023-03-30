namespace Fabron.Models;

[GenerateSerializer]
[Immutable]
public class CronTimer : DistributedTimer<CronTimerSpec>
{ };

[GenerateSerializer]
[Immutable]
public class CronTimerSpec : ISchedulerSpec
{
    [Id(0)]
    public string Schedule { get; init; } = default!;

    [Id(1)]
    public DateTimeOffset? NotBefore { get; set; }

    [Id(2)]
    public DateTimeOffset? ExpirationTime { get; set; }
}

