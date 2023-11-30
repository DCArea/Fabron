using System.Diagnostics;
using System.Diagnostics.Metrics;
using Fabron.Dispatching;
using Fabron.Schedulers;
using InstrumentAggregators;
using Microsoft.Extensions.Logging;

namespace Fabron.Diagnostics;

internal sealed class Telemetry
{
    public const string TelemetryName = "Fabron";
    private static readonly Meter s_meter = new(TelemetryName);
    public static readonly ActivitySource ActivitySource = new(TelemetryName);

    public static readonly CounterAggregator TimerScheduled = new();
    private static readonly ObservableCounter<long> timerScheduledCounter = s_meter.CreateObservableCounter("fabron-timer-scheduled", TimerScheduled.Collect);

    public static HistogramAggregator TimerDispatchTardiness = new(
        new(),
        new(Buckets: [0L, 1L, 5L, 50L, 1_000L, 5_000L, 60_000L]));
    private static readonly ObservableCounter<long> timerDispatchTardinessCount
        = s_meter.CreateObservableCounter("fabron-timer-dispatch-tardiness-count", TimerDispatchTardiness.CollectCount);
    private static readonly ObservableCounter<long> timerDispatchTardinessSum
        = s_meter.CreateObservableCounter("fabron-timer-dispatch-tardiness-sum", TimerDispatchTardiness.CollectSum);
    private static readonly ObservableCounter<long> timerDispatchTardinessBuckets
        = s_meter.CreateObservableCounter("fabron-timer-dispatch-tardiness-buckets", TimerDispatchTardiness.CollectBuckets);


    public static CounterAggregator FabronTimerDispatchedFailed = new();
    private static readonly ObservableCounter<long> timerDispatchFailedCounter = s_meter.CreateObservableCounter("fabron-timer-dispatch-failed", FabronTimerDispatchedFailed.Collect);

    public static HistogramAggregator FabronTimerDispatchDuration = new(
        new(),
        new(Buckets: [0L, 1L, 5L, 50L, 1_000L, 5_000L, 10_000L]));
    private static readonly ObservableCounter<long> timerDispatchDurationCount
        = s_meter.CreateObservableCounter("fabron-timer-dispatch-duration-count", FabronTimerDispatchDuration.CollectCount);
    private static readonly ObservableCounter<long> timerDispatchDurationSum
        = s_meter.CreateObservableCounter("fabron-timer-dispatch-duration-sum", FabronTimerDispatchDuration.CollectSum);
    private static readonly ObservableCounter<long> timerDispatchDurationBuckets
        = s_meter.CreateObservableCounter("fabron-timer-dispatch-duration-buckets", FabronTimerDispatchDuration.CollectBuckets);

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
