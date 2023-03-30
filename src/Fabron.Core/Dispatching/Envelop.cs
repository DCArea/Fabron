using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Fabron.Models;

namespace Fabron.Dispatching;

public record FireEnvelop(
    [property: JsonPropertyName("source")]
    string Source,
    [property: JsonPropertyName("time")]
    DateTimeOffset Time,
    [property: JsonPropertyName("data")]
    string? Data,
    [property: JsonPropertyName("ext")]
    Dictionary<string, string> Extensions
)
{
    [JsonPropertyName("specversion")]
    public string SpecVersion { get; } = "1.0";
};

public static class EnvelopeExtensions
{
    public static FireEnvelop ToEnvelop(this Models.PeriodicTimer timer, DateTimeOffset schedule)
    {
        var source = $"periodic.fabron.io/{timer.Metadata.Key}";
        var id = $"{source}/{schedule.ToUnixTimeSeconds()}";
        return new(source, schedule, timer.Data, timer.Metadata.Extensions);
    }

    public static FireEnvelop ToEnvelop(this CronTimer timer, DateTimeOffset schedule)
    {
        var source = $"cron.fabron.io/{timer.Metadata.Key}";
        var id = $"{source}/{schedule.ToUnixTimeSeconds()}";
        return new(source, schedule, timer.Data, timer.Metadata.Extensions);
    }

    public static FireEnvelop ToEnvelop(this GenericTimer timer, DateTimeOffset schedule)
    {
        var source = $"timed.fabron.io/{timer.Metadata.Key}";
        var id = $"{source}/{schedule.ToUnixTimeSeconds()}";
        return new(source, schedule, timer.Data, timer.Metadata.Extensions);
    }
}
