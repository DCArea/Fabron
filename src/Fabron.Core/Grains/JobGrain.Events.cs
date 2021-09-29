using System;
using System.Threading.Tasks;
using Fabron.Events;
using Fabron.Models;
using Microsoft.Toolkit.Diagnostics;
using Orleans;

namespace Fabron.Grains
{
    public partial class JobGrain
    {
        private EventLog CreateEventLog<TEvent>(TEvent @event, string type) where TEvent : class
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
                throw;
                // reset state
            }
            TransitionState(eventLog);
            _logger.EventRaised(eventLog);
            await NotifyConsumer();
        }

        private async Task RaiseAsync<TEvent>(TEvent @event, string eventType)
            where TEvent : class, IJobEvent
        {
            EventLog eventLog = CreateEventLog(@event, eventType);
            await CommitAsync(eventLog);
        }

        private async Task RaiseAsync(JobScheduled jobScheduled)
        {
            await RaiseAsync(jobScheduled, nameof(JobScheduled));
            MetricsHelper.JobCount_Scheduled.Inc();
        }

        private async Task RaiseAsync(JobExecutionStarted jobExecutionStarted)
        {
            await RaiseAsync(jobExecutionStarted, nameof(JobExecutionStarted));
            MetricsHelper.JobCount_Running.Inc();
        }

        private async Task RaiseAsync(JobExecutionSucceed jobExecutionSucceed)
        {
            await RaiseAsync(jobExecutionSucceed, nameof(JobExecutionSucceed));
            MetricsHelper.JobCount_RanToCompletion.Inc();
        }

        private async Task RaiseAsync(JobExecutionFailed jobExecutionFailed)
        {
            await RaiseAsync(jobExecutionFailed, nameof(JobExecutionFailed));
            MetricsHelper.JobCount_Faulted.Inc();
        }

        private void TransitionState(EventLog eventLog)
        {
            var @event = IJobEvent.Get(eventLog);
            _logger.ApplyingEvent(_state?.Version ?? -1, eventLog);
            _state = @event switch
            {
                JobScheduled e => _state.Apply(e, _key, eventLog.Timestamp),
                JobExecutionStarted e => State.Apply(e, eventLog.Timestamp),
                JobExecutionSucceed e => State.Apply(e, eventLog.Timestamp),
                JobExecutionFailed e => State.Apply(e, eventLog.Timestamp),
                JobDeleted e => State.Apply(e),
                _ => ThrowHelper.ThrowInvalidEventName<Job>(eventLog.EntityKey, eventLog.Version, eventLog.Type)
            };
            _logger.AppliedEvent(State.Version, eventLog);
            Guard.IsEqualTo(State.Version, eventLog.Version, nameof(State.Version));
        }
    }

    public static class JobEventsExtensions
    {
        public static Job Apply(this Job? state, JobScheduled @event, string key, DateTime timestamp)
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
                    @event.CommandData),
                JobStatus.Initial,
                state == null ? 0 : state.Version + 1);

        public static Job Apply(this Job state, JobExecutionStarted @event, DateTime timestamp)
            => state with
            {
                Status = state.Status with
                {
                    StartedAt = timestamp,
                    ExecutionStatus = ExecutionStatus.Started
                },
                Version = state.Version + 1
            };

        public static Job Apply(this Job state, JobExecutionSucceed @event, DateTime timestamp)
            => state with
            {
                Status = state.Status with
                {
                    Result = @event.Result,
                    FinishedAt = timestamp,
                    ExecutionStatus = ExecutionStatus.Succeed
                },
                Version = state.Version + 1
            };

        public static Job Apply(this Job state, JobExecutionFailed @event, DateTime timestamp)
            => state with
            {
                Status = state.Status with
                {
                    FinishedAt = timestamp,
                    ExecutionStatus = ExecutionStatus.Faulted,
                    Reason = @event.Reason,
                },
                Version = state.Version + 1
            };

        public static Job Apply(this Job state, JobDeleted @event)
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
