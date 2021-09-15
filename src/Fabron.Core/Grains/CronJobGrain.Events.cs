using System;
using System.Threading.Tasks;
using Fabron.Events;
using Fabron.Models;
using Microsoft.Toolkit.Diagnostics;

namespace Fabron.Grains
{
    public partial class CronJobGrain
    {
        private EventLog CreateEventLog<TEvent>(TEvent @event, string type) where TEvent : class, ICronJobEvent
            => EventLog.Create<TEvent>(
                _id,
                (_state?.Version ?? -1) + 1,
                DateTime.UtcNow,
                type,
                @event);

        private async Task NotifyConsumer()
        {
            long currentVersion = State.Version;
            Guard.IsGreaterThanOrEqualTo(currentVersion, _consumerOffset, nameof(currentVersion));
            await _consumer.NotifyChanged(_consumerOffset, currentVersion);
        }

        private async Task CommitAsync(EventLog eventLog)
        {
            await _eventStore.CommitEventLog(eventLog);
            TransitionState(eventLog);
            _logger.EventRaised(eventLog);
            await NotifyConsumer();
        }

        private async Task RaiseAsync<TEvent>(TEvent @event, string eventType)
            where TEvent : class, ICronJobEvent
        {
            EventLog eventLog = CreateEventLog(@event, eventType);
            await CommitAsync(eventLog);
        }

        private void TransitionState(EventLog eventLog)
        {
            var @event = ICronJobEvent.Get(eventLog);
            _state = @event switch
            {
                CronJobScheduled e => _state.Apply(e, _id, eventLog.Timestamp),
                CronJobSuspended e => State.Apply(e, eventLog.Timestamp),
                CronJobResumed e => State.Apply(e, eventLog.Timestamp),
                CronJobItemsStatusChanged e => State.Apply(e, eventLog.Timestamp),
                CronJobCompleted e => State.Apply(e, eventLog.Timestamp),
                CronJobDeleted e => State.Apply(e),
                _ => ThrowHelper.ThrowInvalidEventName<CronJob>(eventLog.EntityId, eventLog.Version, eventLog.Type)
            };
            Guard.IsEqualTo(State.Version, eventLog.Version, nameof(State.Version));
        }
    }

    public static class CronJobEventsExtensions
    {
        public static CronJob Apply(this CronJob? state, CronJobScheduled @event, string id, DateTime timestamp)
            => new(
                new(id, timestamp, @event.Labels, @event.Annotations),
                new(
                    @event.Schedule,
                    @event.CommandName,
                    @event.CommandData,
                    @event.NotBefore,
                    @event.ExpirationTime,
                    true),
                CronJobStatus.Initial,
                state == null ? 0 : state.Version + 1);

        public static CronJob Apply(this CronJob state, CronJobSuspended @event, DateTime timestamp)
            => state with
            {
                Spec = state.Spec with
                {
                    Suspend = true
                },
                Version = state.Version + 1
            };

        public static CronJob Apply(this CronJob state, CronJobResumed @event, DateTime timestamp)
            => state with
            {
                Spec = state.Spec with
                {
                    Suspend = false
                },
                Version = state.Version + 1
            };
        public static CronJob Apply(this CronJob state, CronJobItemsStatusChanged @event, DateTime timestamp)
            => state with
            {
                Status = state.Status with
                {
                    Jobs = @event.Items
                },
                Version = state.Version + 1
            };

        public static CronJob Apply(this CronJob state, CronJobCompleted @event, DateTime timestamp)
            => state with
            {
                Status = state.Status with
                {
                    CompletionTimestamp = timestamp
                },
                Version = state.Version + 1
            };

        public static CronJob Apply(this CronJob state, CronJobDeleted @event)
            => state with
            {
                Status = state.Status with
                {
                    Deleted = true
                },
                Version = state.Version + 1
            };

    }
}
