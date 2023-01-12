using System.Diagnostics;
using System.Diagnostics.Metrics;
using CloudEventDotNet.Diagnostics.Aggregators;
using Fabron.Events;
using Fabron.Schedulers;
using Microsoft.Extensions.Logging;

namespace Fabron.Diagnostics;

internal class Telemetry
{
    public const string TelemetryName = "Fabron";
    private static readonly Meter s_meter = new(TelemetryName);
    private static readonly ActivitySource s_source = new(TelemetryName);

    public static CounterAggregator CloudEventScheduled = new(s_meter, "fabron-event-scheduled");

    public static HistogramAggregator CloudEventDispatchTardiness = new(
        new(),
        new(Buckets: new[] { 0L, 1L, 5L, 50L, 1_000L, 5_000L, 60_000L }),
        s_meter,
        "fabron-event-dispatch-tardiness");

    public static CounterAggregator FabronEventDispatchedFailed = new(s_meter, "fabron-event-dispatch-failed");

    public static HistogramAggregator FabronEventDispatchDuration = new(
        new(),
        new(Buckets: new[] { 0L, 1L, 5L, 50L, 1_000L, 5_000L, 10_000L }),
        s_meter,
        "fabron-event-dispatch-duration");


    public static void OnFabronEventDispatching(
        ILogger logger,
        string schedulerKey,
        FabronEventEnvelop envelop,
        DateTimeOffset utcNow)
    {
        var scheduledTime = envelop.Time;
        var tardiness = (utcNow - scheduledTime).TotalMilliseconds;
        CloudEventDispatchTardiness.Record((long)tardiness);
        TickerLog.Dispatching(logger, schedulerKey, utcNow, envelop.Time);
    }

    public static void OnFabronEventDispatched(TimeSpan elapsed) => FabronEventDispatchDuration.Record((long)elapsed.TotalMilliseconds);

    public static void OnFabronEventDispatchFailed(
        ILogger logger,
        string schedulerKey,
        Exception exception)
    {
        TickerLog.ErrorOnTicking(logger, schedulerKey, exception);
        FabronEventDispatchedFailed.Add(1);
    }

    public static Activity? OnTicking()
    {
        Activity.Current?.Dispose();
        Activity.Current = null;
        return s_source.StartActivity("Ticking");
    }
}
