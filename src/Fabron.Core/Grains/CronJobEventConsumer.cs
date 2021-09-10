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
    }

    public class CronJobEventConsumer : Grain, IJobEventConsumer
    {
        private readonly ILogger<CronJobEventConsumer> _logger;
        private readonly IJobEventListener _eventListener;
        private readonly IJobEventStore _store;
        private readonly IJobReporter _reporter;
        private long _currentVersion = -1;
        private long _committedVersion = -1;
        public CronJobEventConsumer(
            ILogger<CronJobEventConsumer> logger,
            IJobEventListener eventListener,
            IJobEventStore store,
            IJobReporter reporter)
        {
            _logger = logger;
            _eventListener = eventListener;
            _store = store;
            _reporter = reporter;
        }

        public Task NotifyChanged(long fromVersion, long currentVersion)
        {
            if (currentVersion > _currentVersion)
            {
                _currentVersion = currentVersion;
            }

            if (_currentVersion <= _committedVersion)
            {
                _logger.LogDebug($"CronJobEventConsumer[{this.GetPrimaryKeyString()}]: Skipped since current version was committed");
                return Task.CompletedTask;
            }

            if (fromVersion < _committedVersion)
            {
                fromVersion = _committedVersion;
            }

            return Consume(fromVersion);

        }

        private async Task Consume(long fromVersion)
        {
            string jobId = this.GetPrimaryKeyString();
            List<EventLog> eventLogs = await _store.GetEventLogs(jobId, fromVersion);
            long offset = _committedVersion;
            foreach (EventLog eventLog in eventLogs)
            {
                IJobEvent jobEvent = eventLog.Type switch
                {
                    nameof(JobScheduled)
                        => eventLog.GetPayload<JobScheduled>(),
                    nameof(JobExecutionStarted)
                        => eventLog.GetPayload<JobExecutionStarted>(),
                    nameof(JobExecutionSucceed)
                        => eventLog.GetPayload<JobExecutionSucceed>(),
                    nameof(JobExecutionFailed)
                        => eventLog.GetPayload<JobExecutionFailed>(),
                    _ => ThrowHelper.ThrowInvalidEventName<IJobEvent>(eventLog.EntityId, eventLog.Version, eventLog.Type)
                };

                await _eventListener.On(jobId, eventLog.Timestamp, jobEvent);
                offset = eventLog.Version;
            }

            ICronJobGrain grain = GrainFactory.GetGrain<ICronJobGrain>(jobId);
            Models.CronJob? state = await grain.GetState();
            Guard.IsNotNull(state, nameof(state));
            await _reporter.Report(state);
            _logger.LogDebug($"[{jobId}]: Job state reported");

            await grain.CommitOffset(offset);
            _committedVersion = offset;
            _logger.LogDebug($"[{jobId}]: Offset committed {offset}");
        }

    }
}
