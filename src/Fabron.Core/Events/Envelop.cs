﻿using System.Text.Json;
using System.Text.Json.Serialization;
using Fabron.Models;

namespace Fabron.Events;

public record FabronEventEnvelop(
    [property: JsonPropertyName("source")]
    string Source,
    [property: JsonPropertyName("time")]
    DateTimeOffset Time,
    [property: JsonPropertyName("data")]
    string? Data
)
{
    [JsonPropertyName("specversion")]
    public string SpecVersion { get; } = "1.0";

    [JsonExtensionData]
    public Dictionary<string, string> Extensions { get; set; } = new();
};

public static class EnvelopeExtensions
{
    public static FabronEventEnvelop ToCloudEvent(this PeriodicEvent @event, DateTimeOffset schedule)
    {
        var source = $"periodic.fabron.io/{@event.Metadata.Key}";
        var id = $"{source}/{schedule.ToUnixTimeSeconds()}";
        return new(source, schedule, @event.Data);
    }

    public static FabronEventEnvelop ToCloudEvent(this CronEvent @event, DateTimeOffset schedule)
    {
        var source = $"cron.fabron.io/{@event.Metadata.Key}";
        var id = $"{source}/{schedule.ToUnixTimeSeconds()}";
        return new(source, schedule, @event.Data);
    }

    public static FabronEventEnvelop ToCloudEvent(this TimedEvent @event, DateTimeOffset schedule)
    {
        var source = $"timed.fabron.io/{@event.Metadata.Key}";
        var id = $"{source}/{schedule.ToUnixTimeSeconds()}";
        return new(source, schedule, @event.Data);
    }
}
