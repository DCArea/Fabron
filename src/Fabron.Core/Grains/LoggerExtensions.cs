using System;
using Fabron.Events;
using Microsoft.Extensions.Logging;

namespace Fabron.Grains
{
    public static class LoggerExtensions
    {
        private static readonly Action<ILogger, string, string, long, string, Exception?> s_eventRaisedDebug;
        private static readonly Action<ILogger, string, string, long, Exception?> s_eventRaisedInformation;
        private static readonly Action<ILogger, string, Exception?> s_startingJobExecution;
        private static readonly Action<ILogger, string, Exception?> s_purged;
        private static readonly Action<ILogger, string, Exception?> s_cancelStatusProberBecauseCronJobDeleted;
        private static readonly Action<ILogger, string, string, long, Exception> s_failedToCommitEvent;
        private static readonly Action<ILogger, string, string, long, long, Exception?> s_applyingEvent;
        private static readonly Action<ILogger, string, string, long, long, Exception?> s_appliedEvent;
        private static readonly Action<ILogger, string, long, Exception?> s_stateSnapshotLoaded;
        private static readonly Action<ILogger, string, long, Exception?> s_loadingEvents;
        private static readonly Action<ILogger, string, long, Exception?> s_consumerOffsetLoaded;
        private static readonly Action<ILogger, string, long, Exception?> s_consumerOffsetUpdated;

        static LoggerExtensions()
        {
            s_eventRaisedDebug = LoggerMessage.Define<string, string, long, string>(
                LogLevel.Debug,
                new EventId(1, nameof(EventRaised)),
                "[{Key}]: Event '{Type}'({Version}) raised, detail: {Data}");
            s_eventRaisedInformation = LoggerMessage.Define<string, string, long>(
                LogLevel.Information,
                new EventId(1, nameof(EventRaised)),
                "[{Key}]: Event '{Type}'({Version}) raised");

            s_startingJobExecution = LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(1, nameof(StartingJobExecution)),
                "[{Key}]: Starting job execution");

            s_purged = LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(1, nameof(Purged)),
                "[{Key}]: Purged");

            s_cancelStatusProberBecauseCronJobDeleted = LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(1, nameof(CancelStatusProberBecauseCronJobDeleted)),
                "[{Key}]: Cancel item status prober because this CronJob was deleted");

            s_failedToCommitEvent = LoggerMessage.Define<string, string, long>(
                LogLevel.Error,
                new EventId(1, nameof(FailedToCommitEvent)),
                "[{Key}]: Failed to commit event '{Type}'({Version})");

            s_applyingEvent = LoggerMessage.Define<string, string, long, long>(
                LogLevel.Debug,
                new EventId(1, nameof(ApplyingEvent)),
                "[{Key}]: Applying event '{Type}'({Version}), current state version: {StateVersion}");

            s_appliedEvent = LoggerMessage.Define<string, string, long, long>(
                LogLevel.Debug,
                new EventId(1, nameof(AppliedEvent)),
                "[{Key}]: Applied event '{Type}'({Version}), current state version: {StateVersion}");

            s_stateSnapshotLoaded = LoggerMessage.Define<string, long>(
                LogLevel.Debug,
                new EventId(1, nameof(StateSnapshotLoaded)),
                "[{Key}]: State snapshot loaded at version: {Version}");

            s_loadingEvents = LoggerMessage.Define<string, long>(
                LogLevel.Debug,
                new EventId(1, nameof(LoadingEvents)),
                "[{Key}]: Loading events from version: {FromVersion}");

            s_consumerOffsetLoaded = LoggerMessage.Define<string, long>(
                LogLevel.Debug,
                new EventId(1, nameof(ConsumerOffsetLoaded)),
                "[{Key}]: Consumer offset loaded at: {Offset}");

            s_consumerOffsetUpdated = LoggerMessage.Define<string, long>(
                LogLevel.Debug,
                new EventId(1, nameof(ConsumerOffsetUpdated)),
                "[{Key}]: Consumer offset updated at: {Offset}");
        }

        public static void EventRaised(this ILogger logger, EventLog eventLog)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                s_eventRaisedDebug(logger, eventLog.EntityKey, eventLog.Type, eventLog.Version, eventLog.Data, null);
            }
            else
            {
                s_eventRaisedInformation(logger, eventLog.EntityKey, eventLog.Type, eventLog.Version, null);
            }
        }

        public static void StartingJobExecution(this ILogger logger, string key)
            => s_startingJobExecution(logger, key, null);

        public static void Purged(this ILogger logger, string key)
            => s_purged(logger, key, null);

        public static void CancelStatusProberBecauseCronJobDeleted(this ILogger logger, string key)
            => s_cancelStatusProberBecauseCronJobDeleted(logger, key, null);

        public static void FailedToCommitEvent(this ILogger logger, EventLog eventLog, Exception e)
            => s_failedToCommitEvent(logger, eventLog.EntityKey, eventLog.Type, eventLog.Version, e);

        public static void ApplyingEvent(this ILogger logger, long stateVersion, EventLog eventLog)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                s_applyingEvent(logger, eventLog.EntityKey, eventLog.Type, eventLog.Version, stateVersion, null);
            }
        }

        public static void AppliedEvent(this ILogger logger, long stateVersion, EventLog eventLog)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                s_appliedEvent(logger, eventLog.EntityKey, eventLog.Type, eventLog.Version, stateVersion, null);
            }
        }

        public static void StateSnapshotLoaded(this ILogger logger, string key, long version)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                s_stateSnapshotLoaded(logger, key, version, null);
            }
        }

        public static void LoadingEvents(this ILogger logger, string key, long fromVersion)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                s_loadingEvents(logger, key, fromVersion, null);
            }
        }

        public static void ConsumerOffsetLoaded(this ILogger logger, string key, long offset)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                s_consumerOffsetLoaded(logger, key, offset, null);
            }
        }

        public static void ConsumerOffsetUpdated(this ILogger logger, string key, long offset)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                s_consumerOffsetUpdated(logger, key, offset, null);
            }
        }


    }
}
