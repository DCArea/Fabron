namespace Fabron.Models;

[GenerateSerializer]
[Immutable]
public class TickerStatus
{
    [Id(0)]
    public DateTimeOffset? StartedAt { get; set; }

    [Id(1)]
    public DateTimeOffset? NextTick { get; set; }

    [Id(1)]
    public Queue<DateTimeOffset> RecentTicks { get; set; } = new();
}
