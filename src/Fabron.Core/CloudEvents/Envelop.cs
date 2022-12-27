using System.Text.Json;
using Fabron.Models;

namespace Fabron.CloudEvents;
public static class EnvelopeExtensions
{
    public static CloudEventEnvelop ToCloudEvent(this PeriodicEvent cron, DateTimeOffset timestamp, JsonSerializerOptions jsonSerializerOptions)
    {
        var template = JsonSerializer.Deserialize<CloudEventTemplate>(cron.Template, jsonSerializerOptions)!;
        var id = $"{cron.Metadata.Key}-{timestamp.ToUnixTimeSeconds()}";
        var source = template.Source ?? "fabron.io/periodicevents";
        var type = template.Type ?? "fabron.periodicevents.fired";
        return ToCloudEvent(template, id, source, type, timestamp);
    }

    public static CloudEventEnvelop ToCloudEvent(this CronEvent cron, DateTimeOffset timestamp, JsonSerializerOptions jsonSerializerOptions)
    {
        var template = JsonSerializer.Deserialize<CloudEventTemplate>(cron.Template, jsonSerializerOptions)!;
        var id = $"{cron.Metadata.Key}-{timestamp.ToUnixTimeSeconds()}";
        var source = template.Source ?? "fabron.io/cronevents";
        var type = template.Type ?? "fabron.cronevents.fired";
        return ToCloudEvent(template, id, source, type, timestamp);
    }

    public static CloudEventEnvelop ToCloudEvent(this TimedEvent schedule, DateTimeOffset timestamp, JsonSerializerOptions jsonSerializerOptions)
    {
        var template = JsonSerializer.Deserialize<CloudEventTemplate>(schedule.Template, jsonSerializerOptions)!;
        var id = $"{schedule.Metadata.Key}-{timestamp.ToUnixTimeSeconds()}";
        var source = template.Source ?? "fabron.io/timedevents";
        var type = template.Type ?? "fabron.timedevents.fired";
        return ToCloudEvent(template, id, source, type, timestamp);
    }

    private static CloudEventEnvelop ToCloudEvent(this CloudEventTemplate template, string id, string source, string type, DateTimeOffset timestamp)
    {
        var envelop = new CloudEventEnvelop(
            Id: id,
            Source: source,
            Type: type,
            Data: template.Data,
            DataSchema: template.DataSchema,
            Subject: template.Subject,
            Time: timestamp
            );
        return envelop;
    }
}
