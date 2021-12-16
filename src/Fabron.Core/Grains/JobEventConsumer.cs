using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron.Events;
using Fabron.Stores;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Placement;

namespace Fabron.Grains
{
    public interface IJobEventConsumer : IGrainWithStringKey
    {
        Task NotifyChanged(long fromVersion, long currentVersion);
        Task Reset();
    }

    [PreferLocalPlacement]
    public class JobEventConsumer : Grain, IJobEventConsumer
    {
        private readonly ILogger<IJobEventConsumer> _logger;
        private readonly IJobEventListener _eventListener;
        private readonly IJobEventStore _store;
        private readonly IJobIndexer _indexer;
        private ConsumerState _state = new();

        public JobEventConsumer(
            ILogger<JobEventConsumer> logger,
            IJobEventListener eventListener,
            IJobEventStore store,
            IJobIndexer indexer)
        {
            _logger = logger;
            _eventListener = eventListener;
            _store = store;
            _indexer = indexer;
        }

        public override Task OnActivateAsync()
        {
            _key = this.GetPrimaryKeyString();
            _grain = GrainFactory.GetGrain<IJobGrain>(_key);
            return Task.CompletedTask;
        }

        private string _key = default!;
        private IJobGrain _grain = default!;

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

            foreach (EventLog eventLog in eventLogs)
            {
                IJobEvent jobEvent = IJobEvent.Get(eventLog);
                try
                {
                    await _eventListener.On(_key, eventLog.Timestamp, jobEvent);
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
            Models.Job? job = await _grain.GetState();
            if (job is null || job.Status.Deleted)
            {
                await _indexer.DeleteJob(_key);
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
