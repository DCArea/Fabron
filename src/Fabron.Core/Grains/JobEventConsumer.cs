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
        private readonly IJobReporter _reporter;
        private long _currentVersion = -1;
        private long _committedVersion = -1;
        public JobEventConsumer(
            ILogger<IJobEventConsumer> logger,
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
                _logger.LogDebug($"JobEventConsumer[{this.GetPrimaryKeyString()}]: Skipped since current version was committed");
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

            IJobGrain grain = GrainFactory.GetGrain<IJobGrain>(jobId);
            Models.Job? state = await grain.GetState();
            Guard.IsNotNull(state, nameof(state));
            await _reporter.Report(state);
            _logger.LogDebug($"JobEventConsumer[{jobId}]: Job state reported");

            await grain.CommitOffset(offset);
            _committedVersion = offset;
            _logger.LogDebug($"JobEventConsumer[{jobId}]: Offset committed {offset}");
        }

    }
}
