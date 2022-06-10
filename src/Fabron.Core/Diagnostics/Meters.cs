using System.Diagnostics.Metrics;

namespace Fabron.Diagnostics;

public static class Meters
{
    public const string MeterName = "Fabron";
    private static readonly Meter s_meter = new(MeterName, "0.0.1");

    public static Counter<int> TimedEventScheduledCount { get; }
        = s_meter.CreateCounter<int>("timedevent-scheduled-count");

    public static Counter<int> CloudEventDispatchCount { get; }
        = s_meter.CreateCounter<int>("cloudevent-dispatch-count");

    public static Counter<int> CloudEventDispatchFailedCount { get; }
        = s_meter.CreateCounter<int>("cloudevent-dispatch-failed-count");

    public static Histogram<double> CloudEventDispatchDuration { get; }
        = s_meter.CreateHistogram<double>("cloudevent-dispatch-duration", "ms");
}
