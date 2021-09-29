using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron.Events;
using Fabron.Stores;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Fabron.Grains
{
    public interface ICronJobEventConsumer : IGrainWithStringKey
    {
        Task NotifyChanged(long fromVersion, long currentVersion);

        Task Reset();
    }

    public class CronJobEventConsumer : Grain, ICronJobEventConsumer
    {
        private readonly ILogger _logger;
        private readonly ICronJobEventListener _eventListener;
        private readonly ICronJobEventStore _store;
        private readonly IJobIndexer _indexer;
        private ConsumerState _state = new();
        public CronJobEventConsumer(
            ILogger<CronJobEventConsumer> logger,
            ICronJobEventListener eventListener,
            ICronJobEventStore store,
            IJobIndexer reporter)
        {
            _logger = logger;
            _eventListener = eventListener;
            _store = store;
            _indexer = reporter;
        }

        public override Task OnActivateAsync()
        {
            _key = this.GetPrimaryKeyString();
            _grain = GrainFactory.GetGrain<ICronJobGrain>(_key);
            return Task.CompletedTask;
        }

        private string _key = default!;
        private ICronJobGrain _grain = default!;

        public Task Reset()
        {
            var newState = new ConsumerState();
            _logger.ResettingConsumerState(_key, _state, newState);
            _state = newState;
            return Task.CompletedTask;
        }

        public async Task NotifyChanged(long committedOffsetFromProducer, long currentVersion)
        {
            if (currentVersion < _state.CommittedOffset || currentVersion < _state.ConsumedOffset)
            {
                await Reset();
            }

            if (currentVersion > _state.CurrentVersion)
            {
                _state = _state with
                {
                    CurrentVersion = currentVersion,
                };
            }

            if (currentVersion <= _state.CommittedOffset)
            {
                _logger.ConsumerIgnoredStateChangedEvent(_key, currentVersion, _state);
                return;
            }

            if (committedOffsetFromProducer > _state.CommittedOffset)
            {
                _state = _state with
                {
                    CommittedOffset = committedOffsetFromProducer,
                };
                _logger.ConsumerUpdatedOffsetFromProducer(_key, committedOffsetFromProducer, _state);
            }

            await Consume();
        }

        private async Task Consume()
        {
            _state = _state with
            {
                ConsumedOffset = _state.CommittedOffset
            };

            var consumeEventsTask = ConsumeEvents();
            var updateIndexTask = UpdateIndex();
            await consumeEventsTask;
            await updateIndexTask;

            await CommitOffset();
        }

        private async Task ConsumeEvents()
        {
            List<EventLog> eventLogs = await _store.GetEventLogs(_key, _state.CommittedOffset);
            if (eventLogs.Count == 0)
            {
                return;
            }

            ICronJobEvent lastEvent;
            foreach (EventLog eventLog in eventLogs)
            {
                lastEvent = ICronJobEvent.Get(eventLog);
                try
                {
                    await _eventListener.On(_key, eventLog.Timestamp, lastEvent);
                }
                catch (Exception e)
                {
                    _logger.ExceptionOnConsumingEvents(_key, eventLog, e);
                }

                _state = _state with
                {
                    ConsumedOffset = eventLog.Version
                };
            }
        }

        private async Task UpdateIndex()
        {
            Models.CronJob? job = await _grain.GetState();
            if (job is null || job.Status.Deleted)
            {
                await _indexer.DeleteCronJob(_key);
                _logger.StateIndexDeleted(_key);
            }
            else
            {
                await _indexer.Index(job);
                _logger.StateIndexed(_key, job.Version);
            }
        }

        private async Task CommitOffset()
        {
            await _grain.CommitOffset(_state.ConsumedOffset);
            _state = _state with
            {
                CommittedOffset = _state.ConsumedOffset
            };
            _logger.ConsumerOffsetCommitted(_key, _state.CommittedOffset);
        }
    }
}
