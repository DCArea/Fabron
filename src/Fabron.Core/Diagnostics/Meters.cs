using System;
using System.Diagnostics.Metrics;

namespace Fabron.Diagnostics;

public static class Meters
{
    public const string MeterName = "Fabron";
    private static readonly Meter s_meter = new(MeterName, "0.0.1");

    public static Counter<int> TimedEventScheduledCount { get; }
        = s_meter.CreateCounter<int>("fabron-timedevent-scheduled-count");

    public static Counter<int> CloudEventDispatchCount { get; }
        = s_meter.CreateCounter<int>("fabron-cloudevent-dispatch-count");

    public static Counter<int> CloudEventDispatchFailedCount { get; }
        = s_meter.CreateCounter<int>("fabron-cloudevent-dispatch-failed-count");

    public static Histogram<double> CloudEventDispatchDuration { get; }
        = s_meter.CreateHistogram<double>("fabron-cloudevent-dispatch-duration", "ms");

    public static Histogram<double> CloudEventDispatchTardiness { get; }
        = s_meter.CreateHistogram<double>("fabron-cloudevent-dispatch-tardiness", "ms");
    public static void RecordCloudEventDispatchTardiness(DateTimeOffset utcNow, DateTimeOffset scheduledTime)
    {
        double tardiness = (utcNow - scheduledTime).TotalMilliseconds;
        CloudEventDispatchTardiness.Record(tardiness);
    }
}
