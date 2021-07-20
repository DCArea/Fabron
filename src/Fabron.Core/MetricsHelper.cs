// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    }
}
