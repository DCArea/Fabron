using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron.Events;
using Fabron.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Diagnostics;
using Orleans;
using Orleans.Concurrency;

namespace Fabron.Grains
{
    public interface ICronJobEventConsumer : IGrainWithStringKey
    {
        [OneWay]
        Task NotifyChanged(long fromVersion, long currentVersion);

        Task Reset();
    }

    public class CronJobEventConsumer : Grain, ICronJobEventConsumer
    {
        private readonly ILogger _logger;
        private readonly ICronJobEventListener _eventListener;
        private readonly ICronJobEventStore _store;
        private readonly IJobIndexer _indexer;
        private long _currentVersion = -1;
        private long _committedOffset = -1;
        private long _consumedOffset = -1;
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
            _currentVersion = -1;
            _committedOffset = -1;
            _consumedOffset = -1;
            return Task.CompletedTask;
        }

        public Task NotifyChanged(long fromVersion, long currentVersion)
        {
            Guard.IsGreaterThanOrEqualTo(currentVersion, _committedOffset, nameof(currentVersion));
            if (currentVersion > _currentVersion)
            {
                _currentVersion = currentVersion;
            }

            if (_currentVersion <= _committedOffset)
            {
                _logger.LogDebug($"CronJobEventConsumer[{_key}]: Skipped since current version was committed");
                return Task.CompletedTask;
            }

            if (fromVersion > _committedOffset)
            {
                _logger.LogDebug($"CronJobEventConsumer[{_key}]: Update invalid _commitedOffset, from {_committedOffset} to {fromVersion}");
                _committedOffset = fromVersion;
            }
            return Consume();

        }

        private async Task Consume()
        {
            _consumedOffset = _committedOffset;
            List<EventLog> eventLogs = await _store.GetEventLogs(_key, _committedOffset);
            if (eventLogs.Count == 0)
            {
                return;
            }

            ICronJobEvent lastEvent;
            foreach (EventLog eventLog in eventLogs)
            {
                lastEvent = ICronJobEvent.Get(eventLog);
                await _eventListener.On(_key, eventLog.Timestamp, lastEvent);
                _consumedOffset = eventLog.Version;
            }

            await UpdateIndex();
            await CommitOffset();
        }

        private async Task UpdateIndex()
        {
            Models.CronJob? state = await _grain.GetState();
            if (state is null || state.Status.Deleted)
            {
                await _indexer.DeleteCronJob(_key);
                _logger.LogDebug($"[{_key}]: CronJob state deleted");
            }
            else
            {
                await _indexer.Index(state);
                _logger.LogDebug($"[{_key}]: CronJob state reported");
            }
        }

        private async Task CommitOffset()
        {
            await _grain.CommitOffset(_consumedOffset);
            _committedOffset = _consumedOffset;
            _logger.LogDebug($"CronJobEventConsumer[{_key}]: Offset committed {_committedOffset}");
        }
    }
}
