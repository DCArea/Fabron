namespace Fabron.Models;

[GenerateSerializer]
[Immutable]
public class TickerStatus
{
    [Id(0)]
    public DateTime? NextTick { get; set; }

    [Id(1)]
    public List<DateTimeOffset> RecentDispatches { get; set; } = new();
}