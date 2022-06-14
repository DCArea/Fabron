using System;
using System.Collections.Generic;
using Orleans;

namespace Fabron.Models;

[GenerateSerializer]
public class TickerStatus
{
    [Id(0)]
    public DateTime? StartAt { get; set; }

    [Id(1)]
    public List<DateTimeOffset> RecentTicks { get; set; } = new();
}
