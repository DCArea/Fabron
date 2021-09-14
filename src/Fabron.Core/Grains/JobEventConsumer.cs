﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron.Events;
using Fabron.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Diagnostics;
using Orleans;
using Orleans.Concurrency;

namespace Fabron.Grains
{
    public interface IJobEventConsumer : IGrainWithStringKey
    {
        [OneWay]
        Task NotifyChanged(long fromVersion, long currentVersion);
    }

    public class JobEventConsumer : Grain, IJobEventConsumer
    {
        private readonly ILogger<IJobEventConsumer> _logger;
        private readonly IJobEventListener _eventListener;
        private readonly IJobEventStore _store;
        private readonly IJobIndexer _indexer;
        private long _currentVersion = -1;
        private long _committedOffset = -1;
        private long _consumedOffset = -1;

        public JobEventConsumer(
            ILogger<IJobEventConsumer> logger,
            IJobEventListener eventListener,
            IJobEventStore store,
            IJobIndexer reporter)
        {
            _logger = logger;
            _eventListener = eventListener;
            _store = store;
            _indexer = reporter;

        }

        public override Task OnActivateAsync()
        {
            _id = this.GetPrimaryKeyString();
            _grain = GrainFactory.GetGrain<IJobGrain>(_id);
            return Task.CompletedTask;
        }

        private string _id = default!;
        private IJobGrain _grain = default!;

        public Task NotifyChanged(long fromVersion, long currentVersion)
        {
            Guard.IsGreaterThanOrEqualTo(currentVersion, _committedOffset, nameof(currentVersion));
            if (currentVersion > _currentVersion)
            {
                _currentVersion = currentVersion;
            }

            if (_currentVersion <= _committedOffset)
            {
                _logger.LogDebug($"JobEventConsumer[{_id}]: Skipped since current version was committed");
                return Task.CompletedTask;
            }

            if (fromVersion >= _committedOffset)
            {
                _committedOffset = fromVersion;
            }
            return Consume();

        }

        private async Task Consume()
        {
            List<EventLog> eventLogs = await _store.GetEventLogs(_id, _committedOffset);
            foreach (EventLog eventLog in eventLogs)
            {
                IJobEvent jobEvent = IJobEvent.Get(eventLog);
                await _eventListener.On(_id, eventLog.Timestamp, jobEvent);
                _consumedOffset = eventLog.Version;
            }

            await UpdateIndex();
            await CommitOffset();

        }

        private async Task UpdateIndex()
        {
            Models.Job? state = await _grain.GetState();
            if (state is null || state.Status.Deleted)
            {
                await _indexer.DeleteJob(_id);
                _logger.LogDebug($"[{_id}]: Job state deleted");
            }
            else
            {
                await _indexer.Index(state);
                _logger.LogDebug($"[{_id}]: Job state reported");
            }
        }

        private async Task CommitOffset()
        {
            await _grain.CommitOffset(_consumedOffset);
            _committedOffset = _consumedOffset;
            _logger.LogDebug($"JobEventConsumer[{_id}]: Offset committed {_committedOffset}");
        }

    }
}
