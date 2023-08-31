using System.Diagnostics;
using System.Diagnostics.Metrics;
using CloudEventDotNet.Diagnostics.Aggregators;
using Fabron.Dispatching;
using Fabron.Schedulers;
using Microsoft.Extensions.Logging;

namespace Fabron.Diagnostics;

internal sealed class Telemetry
{
    public const string TelemetryName = "Fabron";
    private static readonly Meter s_meter = new(TelemetryName);
    public static readonly ActivitySource ActivitySource = new(TelemetryName);

    public static CounterAggregator TimerScheduled = new(s_meter, "fabron-timer-scheduled");

    public static HistogramAggregator TimerDispatchTardiness = new(
        new(),
        new(Buckets: new[] { 0L, 1L, 5L, 50L, 1_000L, 5_000L, 60_000L }),
        s_meter,
        "fabron-timer-dispatch-tardiness");

    public static CounterAggregator FabronTimerDispatchedFailed = new(s_meter, "fabron-timer-dispatch-failed");

    public static HistogramAggregator FabronTimerDispatchDuration = new(
        new(),
        new(Buckets: new[] { 0L, 1L, 5L, 50L, 1_000L, 5_000L, 10_000L }),
        s_meter,
        "fabron-timer-dispatch-duration");


    public static void OnFabronTimerDispatching(
        ILogger logger,
        string schedulerKey,
        FireEnvelop envelop,
        DateTimeOffset utcNow)
    {
        var scheduledTime = envelop.Time;
        var tardiness = (utcNow - scheduledTime).TotalMilliseconds;
        TimerDispatchTardiness.Record((long)tardiness);
        TickerLog.Dispatching(logger, schedulerKey, utcNow, envelop.Time);
    }

    public static void OnFabronTimerDispatched(TimeSpan elapsed) => FabronTimerDispatchDuration.Record((long)elapsed.TotalMilliseconds);

    public static void OnFabronTimerDispatchFailed(
        ILogger logger,
        string schedulerKey,
        Exception exception)
    {
        TickerLog.ErrorOnTicking(logger, schedulerKey, exception);
        FabronTimerDispatchedFailed.Add(1);
    }

    public static Activity? OnTicking()
    {
        Activity.Current?.Dispose();
        Activity.Current = null;
        return ActivitySource.StartActivity("Ticking");
    }
}
