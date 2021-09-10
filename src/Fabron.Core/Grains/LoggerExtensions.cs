using System;
using Fabron.Events;
using Microsoft.Extensions.Logging;

namespace Fabron.Grains
{
    public static class LoggerExtensions
    {
        private static readonly Action<ILogger, string, long, string, string, Exception?> s_eventRaisedDebug;
        private static readonly Action<ILogger, string, long, string, Exception?> s_eventRaisedInformation;
        private static readonly Action<ILogger, string, Exception?> s_startingJobExecution;
        private static readonly Action<ILogger, string, Exception?> s_jobFinalized;

        static LoggerExtensions()
        {
            s_eventRaisedDebug = LoggerMessage.Define<string, long, string, string>(
                LogLevel.Debug,
                new EventId(1, nameof(EventRaised)),
                "Event {Type}({Version}) raised on [{EntityId}], detail: {Data}");
            s_eventRaisedInformation = LoggerMessage.Define<string, long, string>(
                LogLevel.Information,
                new EventId(1, nameof(EventRaised)),
                "Event '{Type}'({Version}) raised on [{EntityId}]");

            s_startingJobExecution = LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(1, nameof(StartingJobExecution)),
                "Job[{JobId}]: Starting job execution");

            s_jobFinalized = LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(1, nameof(JobFinalized)),
                "Job[{JobId}]: Finalized");
        }

        public static void EventRaised(this ILogger logger, EventLog eventLog)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                s_eventRaisedDebug(logger, eventLog.Type, eventLog.Version, eventLog.EntityId, eventLog.Data, null);
            }
            else
            {
                s_eventRaisedInformation(logger, eventLog.Type, eventLog.Version, eventLog.EntityId, null);
            }
        }

        public static void StartingJobExecution(this ILogger logger, string jobId)
            => s_startingJobExecution(logger, jobId, null);

        public static void JobFinalized(this ILogger logger, string jobId)
            => s_jobFinalized(logger, jobId, null);
    }
}
