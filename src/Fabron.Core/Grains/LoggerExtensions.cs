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
        private static readonly Action<ILogger, string, Exception?> s_cronJobSchedulerUnhealthy;
        private static readonly Action<ILogger, string, TimeSpan, Exception?> s_tickerRegistered;
        private static readonly Action<ILogger, string, Exception?> s_tickerStopped;
        private static readonly Action<ILogger, string, Exception?> s_RetryUnregisterReminder;
        private static readonly Action<ILogger, string, Exception?> s_completingCronJob;
        private static readonly Action<ILogger, string, string, Exception?> s_schedulingNewJob;
        private static readonly Action<ILogger, string, string, Exception?> s_scheduledNewJob;
        private static readonly Action<ILogger, string, ConsumerState, ConsumerState, Exception?> s_resettingConsumerState;
        private static readonly Action<ILogger, string, long, ConsumerState, Exception?> s_consumerIgnoredStateChangedEvent;
        private static readonly Action<ILogger, string, long, ConsumerState, Exception?> s_consumerUpdatedOffsetFromProducer;
        private static readonly Action<ILogger, string, long, Exception?> s_stateIndexed;
        private static readonly Action<ILogger, string, Exception?> s_stateIndexDeleted;
        private static readonly Action<ILogger, string, long, Exception?> s_consumerOffsetCommitted;
        private static readonly Action<ILogger, string, string, long, Exception> s_exceptionOnConsumingEvents;

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

            s_cronJobSchedulerUnhealthy = LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(1, nameof(CronJobSchedulerUnhealthy)),
                "[{Key}]: Scheduler unhealthy, restarting");

            s_tickerRegistered = LoggerMessage.Define<string, TimeSpan>(
                LogLevel.Debug,
                new EventId(1, nameof(TickerRegistered)),
                "[{Key}]: Ticker registered with due time: {DueTime}");

            s_tickerStopped = LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(1, nameof(TickerStopped)),
                "[{Key}]: Ticker stopped");

            s_RetryUnregisterReminder = LoggerMessage.Define<string>(
                LogLevel.Warning,
                new EventId(1, nameof(RetryUnregisterReminder)),
                "[{Key}]: Unregister reminder failed, retry");

            s_completingCronJob = LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(1, nameof(CompletingCronJob)),
                "[{Key}]: Completing cron job");

            s_schedulingNewJob = LoggerMessage.Define<string, string>(
                LogLevel.Information,
                new EventId(1, nameof(SchedulingNewJob)),
                "[{Key}]: Scheduling new job[{JobKey}]");

            s_scheduledNewJob = LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                new EventId(1, nameof(ScheduledNewJob)),
                "[{Key}]: Scheduled new job[{JobKey}]");

            s_resettingConsumerState = LoggerMessage.Define<string, ConsumerState, ConsumerState>(
                LogLevel.Information,
                new EventId(1, nameof(ResettingConsumerState)),
                "[{Key}]: Resetting consumer state, before: {OldState}, now: {NewState} ");

            s_consumerIgnoredStateChangedEvent = LoggerMessage.Define<string, long, ConsumerState>(
                LogLevel.Information,
                new EventId(1, nameof(ConsumerIgnoredStateChangedEvent)),
                "[{Key}]: Ignored state change event({CurrentVersion}), consumer state: {state} ");

            s_consumerUpdatedOffsetFromProducer = LoggerMessage.Define<string, long, ConsumerState>(
                LogLevel.Information,
                new EventId(1, nameof(ConsumerUpdatedOffsetFromProducer)),
                "[{Key}]: Updated offset as ({Offset}), consumer state: {state} ");

            s_stateIndexed = LoggerMessage.Define<string, long>(
                LogLevel.Information,
                new EventId(1, nameof(StateIndexed)),
                "[{Key}]: Indexed state at version ({Version})");

            s_stateIndexDeleted = LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(1, nameof(StateIndexDeleted)),
                "[{Key}]: Delete index");

            s_consumerOffsetCommitted = LoggerMessage.Define<string, long>(
                LogLevel.Information,
                new EventId(1, nameof(ConsumerOffsetCommitted)),
                "[{Key}]: Committed offset({offset}) ");

            s_exceptionOnConsumingEvents = LoggerMessage.Define<string, string, long>(
                LogLevel.Error,
                new EventId(1, nameof(ExceptionOnConsumingEvents)),
                "[{Key}]: Exception on consuming event '{EventType}'({EventVersion})");
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

        public static void CronJobSchedulerUnhealthy(this ILogger logger, string key)
        {
            s_cronJobSchedulerUnhealthy(logger, key, null);
        }


        public static void TickerRegistered(this ILogger logger, string key, TimeSpan dueTime)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                s_tickerRegistered(logger, key, dueTime, null);
            }
        }

        public static void TickerStopped(this ILogger logger, string key)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                s_tickerStopped(logger, key, null);
            }
        }

        public static void RetryUnregisterReminder(this ILogger logger, string key)
        {
            s_RetryUnregisterReminder(logger, key, null);
        }

        public static void CompletingCronJob(this ILogger logger, string key)
        {
            s_completingCronJob(logger, key, null);
        }

        public static void SchedulingNewJob(this ILogger logger, string key, string jobKey)
        {
            s_schedulingNewJob(logger, key, jobKey, null);
        }

        public static void ScheduledNewJob(this ILogger logger, string key, string jobKey)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                s_scheduledNewJob(logger, key, jobKey, null);
            }
        }

        public static void ResettingConsumerState(this ILogger logger, string key, ConsumerState oldState, ConsumerState newState)
        {
            s_resettingConsumerState(logger, key, oldState, newState, null);
        }

        public static void ConsumerIgnoredStateChangedEvent(this ILogger logger, string key, long currentVersion, ConsumerState state)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                s_consumerIgnoredStateChangedEvent(logger, key, currentVersion, state, null);
            }
        }

        public static void ConsumerUpdatedOffsetFromProducer(this ILogger logger, string key, long offset, ConsumerState state)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                s_consumerUpdatedOffsetFromProducer(logger, key, offset, state, null);
            }
        }

        public static void StateIndexed(this ILogger logger, string key, long version)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                s_stateIndexed(logger, key, version, null);
            }
        }

        public static void StateIndexDeleted(this ILogger logger, string key)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                s_stateIndexDeleted(logger, key, null);
            }
        }

        public static void ConsumerOffsetCommitted(this ILogger logger, string key, long version)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                s_consumerOffsetCommitted(logger, key, version, null);
            }
        }

        public static void ExceptionOnConsumingEvents(this ILogger logger, string key, EventLog eventLog, Exception e)
        {
            s_exceptionOnConsumingEvents(logger, key, eventLog.Type, eventLog.Version, e);
        }
    }
}
