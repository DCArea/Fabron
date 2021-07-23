// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Orleans.Runtime;

namespace Orleans.TelemetryConsumers.Prometheus
{
    public class PrometheusTelemetryConsumer :
        IEventTelemetryConsumer,
        IExceptionTelemetryConsumer,
        IDependencyTelemetryConsumer,
        IMetricTelemetryConsumer,
        IRequestTelemetryConsumer
    {
        public void Close() { }
        public void DecrementMetric(string name)
            => PrometheusMetrics.GetGauge(name)
                .Dec();

        public void DecrementMetric(string name, double value)
            => PrometheusMetrics.GetGauge(name)
                .Dec(value);

        public void Flush() { }
        public void IncrementMetric(string name)
            => PrometheusMetrics.GetGauge(name)
                .Inc();

        public void IncrementMetric(string name, double value)
            => PrometheusMetrics.GetGauge(name)
                .Inc(value);

        public void TrackDependency(string dependencyName, string commandName, DateTimeOffset startTime, TimeSpan duration, bool success)
            => PrometheusMetrics.GetDependencyHistogram(dependencyName, commandName, success)
                .Observe(duration.TotalSeconds);

        public void TrackEvent(string eventName, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null)
        {
            PrometheusMetrics.GetEventCounter(eventName)
                .Inc();
            if (metrics is not null)
            {
                foreach (KeyValuePair<string, double> item in metrics)
                {
                    PrometheusMetrics.GetEventGauge(item.Key, eventName)
                        .Set(item.Value);
                }
            }
        }

        public void TrackException(Exception exception, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null)
        {
            string exceptionTypeName = exception.GetType().Name;
            PrometheusMetrics.GetExceptionCounter(exceptionTypeName)
                .Inc();
            if (metrics is not null)
            {
                foreach (KeyValuePair<string, double> item in metrics)
                {
                    PrometheusMetrics.GetExceptionGauge(item.Key, exceptionTypeName)
                        .Set(item.Value);
                }
            }
        }

        public void TrackMetric(string name, double value, IDictionary<string, string>? properties = null)
            => PrometheusMetrics.GetGauge(name)
                .Set(value);

        public void TrackMetric(string name, TimeSpan value, IDictionary<string, string>? properties = null)
            => PrometheusMetrics.GetHistogram(name)
                .Observe(value.TotalSeconds);
        public void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode, bool success)
            => PrometheusMetrics.GetRequestHistogram(name, responseCode, success)
                .Observe(duration.TotalSeconds);

    }
}
