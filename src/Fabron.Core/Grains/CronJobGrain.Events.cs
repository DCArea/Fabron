using System;
using System.Threading.Tasks;
using Fabron.Events;
using Fabron.Models;
using Microsoft.Toolkit.Diagnostics;
using Orleans;

namespace Fabron.Grains
{
    public partial class CronJobGrain
    {
        private EventLog CreateEventLog<TEvent>(TEvent @event, string type) where TEvent : class, ICronJobEvent
            => EventLog.Create<TEvent>(
                _key,
                (_state?.Version ?? -1) + 1,
                DateTime.UtcNow,
                type,
                @event);

        private async Task NotifyConsumer()
        {
            long currentVersion = State.Version;
            Guard.IsGreaterThanOrEqualTo(currentVersion, _consumerOffset, nameof(currentVersion));
            if (_options.UseAsynchronousIndexer)
            {
                _consumer.InvokeOneWay(c => c.NotifyChanged(_consumerOffset, currentVersion));
            }
            else
            {
                await _consumer.NotifyChanged(_consumerOffset, currentVersion);
            }
        }

        private async Task CommitAsync(EventLog eventLog)
        {
            try
            {
                await _eventStore.CommitEventLog(eventLog);
            }
            catch (Exception e)
            {
                _logger.FailedToCommitEvent(eventLog, e);
                // reset state
            }
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
            _logger.ApplyingEvent(_state?.Version ?? -1, eventLog);
            _state = @event switch
            {
                CronJobScheduled e => _state.Apply(e, _key, eventLog.Timestamp),
                CronJobSuspended e => State.Apply(e, eventLog.Timestamp),
                CronJobResumed e => State.Apply(e, eventLog.Timestamp),
                CronJobItemsStatusChanged e => State.Apply(e, eventLog.Timestamp),
                CronJobCompleted e => State.Apply(e, eventLog.Timestamp),
                CronJobDeleted e => State.Apply(e),
                _ => ThrowHelper.ThrowInvalidEventName<CronJob>(eventLog.EntityKey, eventLog.Version, eventLog.Type)
            };
            _logger.AppliedEvent(State.Version, eventLog);
            Guard.IsEqualTo(State.Version, eventLog.Version, nameof(State.Version));
        }
    }

    public static class CronJobEventsExtensions
    {
        public static CronJob Apply(this CronJob? state, CronJobScheduled @event, string key, DateTime timestamp)
            => new(
                new(
                    key,
                    state == null ? Guid.NewGuid().ToString() : state.Metadata.Uid,
                    timestamp,
                    @event.Labels,
                    @event.Annotations),
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
