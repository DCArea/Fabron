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
                this.GetPrimaryKeyString(),
                (_state?.Version ?? -1) + 1,
                DateTime.UtcNow,
                type,
                @event);

        private async Task CommitAsync(EventLog eventLog)
        {
            TransitionState(eventLog);
            await _eventStore.CommitEventLog(eventLog);
            _logger.EventRaised(eventLog);
            await _consumer.NotifyChanged(_offset, eventLog.Version);
        }

        private async Task RaiseAsync<TEvent>(TEvent @event, string eventType)
            where TEvent : class, ICronJobEvent
        {
            EventLog eventLog = CreateEventLog(@event, eventType);
            await CommitAsync(eventLog);
        }

        private void TransitionState(EventLog eventlog)
        {
            var @event = ICronJobEvent.Get(eventlog);
            _state = @event switch
            {
                CronJobScheduled e => _state.Apply(e, this.GetPrimaryKeyString(), eventlog.Timestamp),
                CronJobSuspended e => State.Apply(e, eventlog.Timestamp),
                CronJobResumed e => State.Apply(e, eventlog.Timestamp),
                CronJobCompleted e => State.Apply(e, eventlog.Timestamp),
                _ => ThrowHelper.ThrowInvalidEventName<CronJob>(eventlog.EntityId, eventlog.Version, eventlog.Type)
            };
            Guard.IsEqualTo(State.Version, eventlog.Version, nameof(State.Version));
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
                0);

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

        public static CronJob Apply(this CronJob state, CronJobCompleted @event, DateTime timestamp)
            => state with
            {
                Status = state.Status with
                {
                    CompletionTimestamp = timestamp
                },
                Version = state.Version + 1
            };

    }
}
