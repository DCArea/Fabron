
using System;
using System.Diagnostics;
using Prometheus;

namespace Fabron;

public static class MetricsHelper
{
    public static readonly Counter JobCount = Metrics
        .CreateCounter("fabron_jobs_total", "Number of jobs.", new CounterConfiguration
        {
            LabelNames = new[] { "status" }
        });

    public static readonly Counter.Child JobCount_Scheduled = JobCount
        .WithLabels(new[] { "scheduled" });

    public static readonly Counter.Child JobCount_Running = JobCount
        .WithLabels(new[] { "running" });

    public static readonly Counter.Child JobCount_Completed = JobCount
        .WithLabels(new[] { "completed" });

    public static readonly Counter.Child JobCount_Faulted = JobCount
        .WithLabels(new[] { "faulted" });

    public static readonly Histogram JobScheduleTardiness = Metrics
        .CreateHistogram("fabron_jobs_schedule_tardiness_seconds", "Job schedule tardiness.", new HistogramConfiguration
        {
            Buckets = new double[14] { 0.5, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233, 377 }
        });

    public static readonly Histogram JobExecutionDuration = Metrics
        .CreateHistogram("fabron_jobs_execution_duration_seconds", "Job execution duration.", new HistogramConfiguration
        {
            Buckets = new double[12] { 0.01, 0.05, 0.1, 0.3, 0.5, 1, 2, 3, 5, 8, 13, 21 }
        });
}


internal struct ValueStopwatch
{
    private static readonly double s_timestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

    private readonly long _startTimestamp;

    public bool IsActive => _startTimestamp != 0;

    private ValueStopwatch(long startTimestamp)
    {
        _startTimestamp = startTimestamp;
    }

    public static ValueStopwatch StartNew() => new(Stopwatch.GetTimestamp());

    public TimeSpan GetElapsedTime()
    {
        // Start timestamp can't be zero in an initialized ValueStopwatch. It would have to be literally the first thing executed when the machine boots to be 0.
        // So it being 0 is a clear indication of default(ValueStopwatch)
        if (!IsActive)
        {
            throw new InvalidOperationException("An uninitialized, or 'default', ValueStopwatch cannot be used to get elapsed time.");
        }

        long end = Stopwatch.GetTimestamp();
        long timestampDelta = end - _startTimestamp;
        long ticks = (long)(s_timestampToTicks * timestampDelta);
        return new TimeSpan(ticks);
    }
}
