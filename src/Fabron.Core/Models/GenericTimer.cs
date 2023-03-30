namespace Fabron.Models;

[GenerateSerializer]
[Immutable]
public class GenericTimer : DistributedTimer<GenericTimerSpec>
{ }

[GenerateSerializer]
[Immutable]
public class GenericTimerSpec : ISchedulerSpec
{
    [Id(0)]
    public DateTimeOffset Schedule { get; init; } = default!;
}

