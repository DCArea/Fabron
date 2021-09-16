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
        private static readonly Action<ILogger, string, Exception?> s_jobPurged;
        private static readonly Action<ILogger, string, Exception?> s_cronJobPurged;
        private static readonly Action<ILogger, string, Exception?> s_cancelStatusProberBecauseCronJobDeleted;
        private static readonly Action<ILogger, string, string, long, Exception> s_failedToCommitEvent;

        static LoggerExtensions()
        {
            s_eventRaisedDebug = LoggerMessage.Define<string, long, string, string>(
                LogLevel.Debug,
                new EventId(1, nameof(EventRaised)),
                "Event '{Type}'({Version}) raised on [{Key}], detail: {Data}");
            s_eventRaisedInformation = LoggerMessage.Define<string, long, string>(
                LogLevel.Information,
                new EventId(1, nameof(EventRaised)),
                "Event '{Type}'({Version}) raised on [{Key}]");

            s_startingJobExecution = LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(1, nameof(StartingJobExecution)),
                "Job[{Key}]: Starting job execution");

            s_jobPurged = LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(1, nameof(JobPurged)),
                "Job[{Key}]: Purged");

            s_cronJobPurged = LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(1, nameof(CronJobPurged)),
                "CronJob[{Key}]: Purged");

            s_cancelStatusProberBecauseCronJobDeleted = LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(1, nameof(CancelStatusProberBecauseCronJobDeleted)),
                "CronJob[{Key}]: Cancel item status prober because this CronJob was deleted");

            s_failedToCommitEvent = LoggerMessage.Define<string, string, long>(
                LogLevel.Error,
                new EventId(1, nameof(FailedToCommitEvent)),
                "CronJob[{Key}]: Failed to commit event '{Type}'({Version})");
        }

        public static void EventRaised(this ILogger logger, EventLog eventLog)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                s_eventRaisedDebug(logger, eventLog.Type, eventLog.Version, eventLog.EntityKey, eventLog.Data, null);
            }
            else
            {
                s_eventRaisedInformation(logger, eventLog.Type, eventLog.Version, eventLog.EntityKey, null);
            }
        }

        public static void StartingJobExecution(this ILogger logger, string key)
            => s_startingJobExecution(logger, key, null);

        public static void JobPurged(this ILogger logger, string key)
            => s_jobPurged(logger, key, null);

        public static void CronJobPurged(this ILogger logger, string key)
            => s_cronJobPurged(logger, key, null);

        public static void CancelStatusProberBecauseCronJobDeleted(this ILogger logger, string key)
            => s_cancelStatusProberBecauseCronJobDeleted(logger, key, null);

        public static void FailedToCommitEvent(this ILogger logger, EventLog eventLog, Exception e)
            => s_failedToCommitEvent(logger, eventLog.EntityKey, eventLog.Type, eventLog.Version, e);
    }
}
