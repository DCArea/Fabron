using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

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
