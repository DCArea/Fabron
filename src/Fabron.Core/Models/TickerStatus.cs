using System;
using System.Collections.Generic;
using Orleans;

namespace Fabron.Models;

[GenerateSerializer]
public class TickerStatus
{
    [Id(0)]
    public DateTime? NextTick { get; set; }

    [Id(1)]
    public List<DateTimeOffset> RecentDispatches { get; set; } = new();
}
