
using Prometheus;

namespace Fabron
{
    public static class MetricsHelper
    {
        public static readonly Counter JobCount = Metrics
            .CreateCounter("fabron_jobs_total", "Number of jobs.", new CounterConfiguration
            {
                LabelNames = new[] { "status" }
            });

        public static readonly Counter.Child JobCount_Created = JobCount
            .WithLabels(new[] { "created" });

        public static readonly Counter.Child JobCount_Scheduled = JobCount
            .WithLabels(new[] { "scheduled" });

        public static readonly Counter.Child JobCount_Running = JobCount
            .WithLabels(new[] { "Running" });

        public static readonly Counter.Child JobCount_RanToCompletion = JobCount
            .WithLabels(new[] { "ran_to_completion" });

        public static readonly Counter.Child JobCount_Faulted = JobCount
            .WithLabels(new[] { "faulted" });

        public static readonly Counter.Child JobCount_Canceled = JobCount
            .WithLabels(new[] { "canceled" });

        public static readonly Histogram JobScheduleTardiness = Metrics
            .CreateHistogram("fabron_jobs_schedule_tardiness", "Job schedule tardiness.", new HistogramConfiguration
            {
                Buckets = new double[14]
                {
                    0.5,
                    1,
                    2,
                    3,
                    5,
                    8,
                    13,
                    21,
                    34,
                    55,
                    89,
                    144,
                    233,
                    377
                }
            });
    }
}
