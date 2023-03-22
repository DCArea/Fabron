// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;

#nullable disable

namespace Microsoft.Extensions.Logging.Console
{
    internal sealed class EnrichedJsonConsoleFormatter : ConsoleFormatter, IDisposable
    {
        public const string FormatterName = "ejson";
        private const string OriginalFormatFieldName = "{OriginalFormat}";
        private readonly IDisposable _optionsReloadToken;

        public EnrichedJsonConsoleFormatter(IOptionsMonitor<EnrichedJsonConsoleFormatterOptions> options)
            : base(FormatterName)
        {
            ReloadLoggerOptions(options.CurrentValue);
            _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
        }

        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter)
        {
            string message = logEntry.Formatter(logEntry.State, logEntry.Exception);
            if (logEntry.Exception == null && message == null)
            {
                return;
            }
            LogLevel logLevel = logEntry.LogLevel;
            string category = logEntry.Category;
            Exception exception = logEntry.Exception;
            const int DefaultBufferSize = 1024;
            DateTime timestamp = DateTime.UtcNow;
            using (var output = new PooledByteBufferWriter(DefaultBufferSize))
            {
                using (var writer = new Utf8JsonWriter(output, FormatterOptions.JsonWriterOptions))
                {
                    writer.WriteStartObject();
                    writer.WriteString("Timestamp", timestamp.ToString("o"));
                    writer.WriteString(nameof(logEntry.LogLevel), GetLogLevelString(logLevel));
                    writer.WriteString(nameof(logEntry.Category), category);
                    writer.WriteString("Message", message);

                    if (exception != null)
                    {
                        string exceptionMessage = exception.ToString();
                        if (!FormatterOptions.JsonWriterOptions.Indented)
                        {
                            exceptionMessage = exceptionMessage.Replace(Environment.NewLine, " ");
                        }
                        writer.WriteString(nameof(Exception), exceptionMessage);
                    }

                    var activity = Activity.Current;
                    if (activity != null)
                    {
                        writer.WriteString(nameof(activity.TraceId), activity.TraceId.ToString());
                        writer.WriteString(nameof(activity.SpanId), activity.SpanId.ToString());

                        var tags = new ActivityTagsCollection
                        {
                            { nameof(logEntry.Category), logEntry.Category },
                            { nameof(logEntry.LogLevel), logEntry.LogLevel },
                            { "Message", message }
                        };
                        var activityEvent = new ActivityEvent("log", timestamp, tags);
                        activity.AddEvent(activityEvent);
                        if (logEntry.Exception != null)
                        {
                            activity.RecordException(logEntry.Exception);
                        }
                    }

                    writer.WriteEndObject();
                    writer.Flush();
                }
                textWriter.Write(Encoding.UTF8.GetString(output.WrittenMemory.Span));
            }
            textWriter.Write(Environment.NewLine);
        }

        private static string GetLogLevelString(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => "Trace",
                LogLevel.Debug => "Debug",
                LogLevel.Information => "Information",
                LogLevel.Warning => "Warning",
                LogLevel.Error => "Error",
                LogLevel.Critical => "Critical",
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
            };
        }

        private static string ToInvariantString(object obj) => Convert.ToString(obj, CultureInfo.InvariantCulture);

        internal EnrichedJsonConsoleFormatterOptions FormatterOptions { get; set; }

        private void ReloadLoggerOptions(EnrichedJsonConsoleFormatterOptions options)
        {
            FormatterOptions = options;
        }

        public void Dispose()
        {
            _optionsReloadToken?.Dispose();
        }
    }
}
