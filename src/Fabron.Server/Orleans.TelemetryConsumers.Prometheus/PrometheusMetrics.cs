
using System.Collections.Concurrent;

using Prometheus;

namespace Orleans.TelemetryConsumers.Prometheus
{
    public static class PrometheusMetrics
    {
        private static readonly string[] s_dependencyMetricsLabelNames = new string[] { "dependency_name", "command_name", "success" };
        private static readonly string[] s_requestMetricsLabelNames = new string[] { "name", "response_code", "success" };
        private static readonly string[] s_eventMetricsLabelNames = new string[] { "event_name" };
        private static readonly string[] s_exceptionMetricsLabelNames = new string[] { "exception_name" };

        private static readonly Histogram s_dependencyHistogram = Metrics.CreateHistogram("orleans_dependency_duration_seconds", "Duration of denpendency", new HistogramConfiguration
        {
            LabelNames = s_dependencyMetricsLabelNames
        });

        private static readonly Histogram s_requestHistogram = Metrics.CreateHistogram("orleans_request_duration_seconds", "Duration of request", new HistogramConfiguration
        {
            LabelNames = s_requestMetricsLabelNames
        });

        private static readonly Counter s_eventCounter = Metrics.CreateCounter("orleans_events_total", "Total count of events", new CounterConfiguration
        {
            LabelNames = s_eventMetricsLabelNames
        });
        private static readonly Counter s_exceptionCounter = Metrics.CreateCounter("orleans_exceptions_total", "Total count of exceptions", new CounterConfiguration
        {
            LabelNames = s_exceptionMetricsLabelNames
        });

        private static readonly ConcurrentDictionary<string, Gauge> s_gauges = new();
        private static readonly ConcurrentDictionary<string, Histogram> s_histogram = new();

        public static Gauge GetGauge(string name)
            => s_gauges.GetOrAdd(name, key => Metrics.CreateGauge(FormatMetricName(key), "Gauge for " + key));

        public static Histogram GetHistogram(string name)
            => s_histogram.GetOrAdd(name, key => Metrics.CreateHistogram(FormatMetricName(key), "Histogram for " + key));

        public static Histogram.Child GetDependencyHistogram(string dependencyName, string commandName, bool success)
            => s_dependencyHistogram
                .WithLabels(dependencyName, commandName, success.ToString());

        public static Histogram.Child GetRequestHistogram(string requestName, string responseCode, bool success)
            => s_requestHistogram
                .WithLabels(requestName, responseCode, success.ToString());

        public static Counter.Child GetEventCounter(string eventName)
            => s_eventCounter.WithLabels(eventName);
        public static Gauge.Child GetEventGauge(string metricName, string eventName)
            => s_gauges.GetOrAdd(metricName, key => Metrics.CreateGauge(FormatEventMetricName(key), "Event Gauge for " + key, new GaugeConfiguration
            {
                LabelNames = s_eventMetricsLabelNames
            }))
                .WithLabels(eventName);

        public static Counter.Child GetExceptionCounter(string exceptionName)
            => s_exceptionCounter.WithLabels(exceptionName);
        public static Gauge.Child GetExceptionGauge(string metricName, string exceptionName)
            => s_gauges.GetOrAdd(metricName, key => Metrics.CreateGauge(FormatExceptionMetricName(key), "Exception Gauge for " + key, new GaugeConfiguration
            {
                LabelNames = s_exceptionMetricsLabelNames
            }))
                .WithLabels(exceptionName);

        private static string FormatMetricName(string name) => "orleans_" + name.ToLower().Replace(".", "_");
        private static string FormatEventMetricName(string name) => "orleans_events_" + name.ToLower().Replace(".", "_");
        private static string FormatExceptionMetricName(string name) => "orleans_exception_" + name.ToLower().Replace(".", "_");

    }
}
