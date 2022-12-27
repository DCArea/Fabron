using System.Diagnostics;
using System.Diagnostics.Metrics;
using CloudEventDotNet.Diagnostics.Aggregators;
using Fabron.CloudEvents;
using Fabron.Schedulers;
using Microsoft.Extensions.Logging;

namespace Fabron.Diagnostics;

internal class Telemetry
{
    public const string TelemetryName = "Fabron";
    private static readonly Meter s_meter = new(TelemetryName);
    private static readonly ActivitySource s_source = new(TelemetryName);

    public static CounterAggregator CloudEventScheduled = new(s_meter, "fabron-cloudevent-scheduled");

    public static HistogramAggregator CloudEventDispatchTardiness = new(
        new(),
        new(Buckets: new[] { 0L, 1L, 5L, 50L, 1_000L, 5_000L, 60_000L }),
        s_meter,
        "fabron-cloudevent-dispatch-tardiness");

    public static CounterAggregator CloudEventDispatchedFailed = new(s_meter, "fabron-cloudevent-dispatch-failed");

    public static HistogramAggregator CloudEventDispatchDuration = new(
        new(),
        new(Buckets: new[] { 0L, 1L, 5L, 50L, 1_000L, 5_000L, 10_000L }),
        s_meter,
        "fabron-cloudevent-dispatch-duration");


    public static void OnCloudEventDispatching(
        ILogger logger,
        string schedulerKey,
        CloudEventEnvelop cloudEvent,
        DateTimeOffset utcNow)
    {
        var scheduledTime = cloudEvent.Time;
        var tardiness = (utcNow - scheduledTime).TotalMilliseconds;
        CloudEventDispatchTardiness.Record((long)tardiness);
        TickerLog.Dispatching(logger, schedulerKey, utcNow, cloudEvent.Time);
    }

    public static void OnCloudEventDispatched(TimeSpan elapsed) => CloudEventDispatchDuration.Record((long)elapsed.TotalMilliseconds);

    public static void OnCloudEventDispatchFailed(
        ILogger logger,
        string schedulerKey,
        Exception exception)
    {
        TickerLog.ErrorOnTicking(logger, schedulerKey, exception);
        CloudEventDispatchedFailed.Add(1);
    }

    public static Activity? OnTicking()
    {
        Activity.Current?.Dispose();
        Activity.Current = null;
        return s_source.StartActivity("Ticking");
    }
}
