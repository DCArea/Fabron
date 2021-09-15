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
            _id = this.GetPrimaryKeyString();
            _grain = GrainFactory.GetGrain<ICronJobGrain>(_id);
            return Task.CompletedTask;
        }

        private string _id = default!;
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
                _logger.LogDebug($"CronJobEventConsumer[{_id}]: Skipped since current version was committed");
                return Task.CompletedTask;
            }

            if (fromVersion > _committedOffset)
            {
                _logger.LogDebug($"CronJobEventConsumer[{_id}]: Update invalid _commitedOffset, from {_committedOffset} to {fromVersion}");
                _committedOffset = fromVersion;
            }
            return Consume();

        }

        private async Task Consume()
        {
            _consumedOffset = _committedOffset;
            List<EventLog> eventLogs = await _store.GetEventLogs(_id, _committedOffset);
            if (eventLogs.Count == 0)
            {
                return;
            }

            ICronJobEvent lastEvent;
            foreach (EventLog eventLog in eventLogs)
            {
                lastEvent = ICronJobEvent.Get(eventLog);
                await _eventListener.On(_id, eventLog.Timestamp, lastEvent);
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
                await _indexer.DeleteCronJob(_id);
                _logger.LogDebug($"[{_id}]: CronJob state deleted");
            }
            else
            {
                await _indexer.Index(state);
                _logger.LogDebug($"[{_id}]: CronJob state reported");
            }
        }

        private async Task CommitOffset()
        {
            await _grain.CommitOffset(_consumedOffset);
            _committedOffset = _consumedOffset;
            _logger.LogDebug($"CronJobEventConsumer[{_id}]: Offset committed {_committedOffset}");
        }
    }
}
