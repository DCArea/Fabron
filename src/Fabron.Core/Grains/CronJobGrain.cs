
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabron.Events;
using Fabron.Models;
using Fabron.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Diagnostics;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Fabron.Grains
{
    public interface ICronJobGrain : IGrainWithStringKey
    {
        [ReadOnly]
        Task<CronJob?> GetState();

        Task Schedule(
            string cronExp,
            string commandName,
            string commandData,
            DateTime? start,
            DateTime? end,
            bool suspend,
            Dictionary<string, string>? labels,
            Dictionary<string, string>? annotations);

        Task Trigger();

        [AlwaysInterleave]
        Task Delete();

        Task Suspend();

        Task Resume();

        [AlwaysInterleave]
        Task CommitOffset(long version);

        [AlwaysInterleave]
        Task Purge();

        [AlwaysInterleave]
        Task WaitEventsConsumed();
    }

    public partial class CronJobGrain : Grain, ICronJobGrain, IRemindable
    {
        private readonly ILogger _logger;
        private readonly ICronJobEventStore _eventStore;
        private IGrainReminder? _tickReminder;
        private IDisposable? _tickTimer;
        private IDisposable? _statusProber;
        public CronJobGrain(
            ILogger<CronJobGrain> logger,
            ICronJobEventStore eventStore)
        {
            _logger = logger;
            _eventStore = eventStore;

        }

        public override async Task OnActivateAsync()
        {
            _id = this.GetPrimaryKeyString();
            _consumer = GrainFactory.GetGrain<ICronJobEventConsumer>(_id);

            List<EventLog> eventLogs = await _eventStore.GetEventLogs(_id, 0);
            foreach (EventLog? eventLog in eventLogs)
            {
                TransitionState(eventLog);
            }

            _consumerOffset = await _eventStore.GetConsumerOffset(_id);
        }

        private string _id = default!;
        private ICronJobEventConsumer _consumer = default!;
        private long _consumerOffset;
        private CronJob? _state;
        private TaskCompletionSource<bool>? _consumingCompletionSource;

        private CronJob State
        {
            get
            {
                Guard.IsNotNull(_state, nameof(State));
                return _state;
            }
        }
        private bool ConsumerNotFollowedUp => _state is not null && _state.Version != _consumerOffset;
        private bool Deleted => _state is null || _state.Status.Deleted;
        private bool DeletedButNotPurged => (_state is not null && _state.Status.Deleted) || (_state is null && _consumerOffset != -1);

        public Task<CronJob?> GetState() => Task.FromResult(_state);

        public async Task Purge()
        {
            if (ConsumerNotFollowedUp)
            {
                await NotifyConsumer();
                await WaitEventsConsumed();
                return;
            }

            if(_state != null)
            {
                await _eventStore.ClearEventLogs(_id, long.MaxValue);
                _state = null;
            }
            if(_consumerOffset != -1)
            {
                await _eventStore.ClearConsumerOffset(_id);
                await _consumer.Reset();
                _consumerOffset = -1;
            }
            await StopTicker();
        }

        public async Task Delete()
        {
            //if (Deleted)
            //{
            //    return;
            //}
            await TickAfter(TimeSpan.FromSeconds(20));
            CronJobDeleted? @event = new CronJobDeleted();
            await RaiseAsync(@event, nameof(CronJobDeleted));
        }

        public async Task Schedule(
            string cronExp,
            string commandName,
            string commandData,
            DateTime? notBefore,
            DateTime? expirationTime,
            bool suspend,
            Dictionary<string, string>? labels,
            Dictionary<string, string>? annotations)
        {
            if (ConsumerNotFollowedUp)
            {
                await NotifyConsumer();
                ThrowHelper.ThrowConsumerNotFollowedUp(_id, State.Version, _consumerOffset);
            }

            var @event = new CronJobScheduled(
                labels ?? new Dictionary<string, string>(),
                annotations ?? new Dictionary<string, string>(),
                cronExp,
                commandName,
                commandData,
                notBefore,
                expirationTime
            );
            await RaiseAsync(@event, nameof(CronJobScheduled));

            if (!suspend)
            {
                await Resume();
            }
        }

        public async Task Trigger() => await ScheduleJob();


        public async Task Suspend()
        {
            var @event = new CronJobSuspended();
            await RaiseAsync(@event, nameof(CronJobSuspended));
            await StopTicker();
        }

        public async Task Resume()
        {
            await ScheduleNextTick();
            var @event = new CronJobResumed();
            await RaiseAsync(@event, nameof(CronJobResumed));
        }

        private async Task Tick()
        {
            if (DeletedButNotPurged)
            {
                await Purge();
                return;
            }

            DateTime now = DateTime.UtcNow;
            DateTime notBefore = now.AddSeconds(-5);
            DateTime? tick = State.GetNextTick(notBefore);
            if (tick is null)
            {
                await TryComplete();
            }
            else
            {
                if (tick.Value <= now.AddSeconds(5))
                {
                    await ScheduleJob();
                }

                await ScheduleNextTick();
            }
        }

        private async Task CheckJobStatus()
        {
            IEnumerable<Task<JobItem>> checkJobStatusTasks = State.Status.Jobs
                .Select(job => Check(job));
            List<JobItem>? jobItems = (await Task.WhenAll(checkJobStatusTasks)).ToList();

            var @event = new CronJobItemsStatusChanged(jobItems.TakeLast(10).ToList());
            await RaiseAsync(@event, nameof(CronJobItemsStatusChanged));

            if (!State.HasRunningJobs)
            {
                StopProbeTimer();
            }

            async Task<JobItem> Check(JobItem job)
            {
                if (job.Status is ExecutionStatus.Succeed or ExecutionStatus.Faulted)
                {
                    return job;
                }
                string? jobId = GetChildJobIdByIndex(job.Index);
                IJobGrain? grain = GrainFactory.GetGrain<IJobGrain>(jobId);
                ExecutionStatus status = await grain.GetStatus();
                return job with
                {
                    Status = status
                };
            }
        }


        private async Task ScheduleJob()
        {
            JobItem? latestJob = State.LatestItem;
            uint latestIndex = latestJob is null ? 0 : latestJob.Index;
            JobItem? jobItem = await Schedule(latestIndex + 1);
            List<JobItem> items = State.Status.Jobs;
            items.Add(jobItem);

            var @event = new CronJobItemsStatusChanged(items.TakeLast(10).ToList());
            await RaiseAsync(@event, nameof(CronJobItemsStatusChanged));

            EnsureStatusProber();
            _logger.LogDebug($"CronJob[{State.Metadata.Uid}]: Scheduled job-{jobItem.Index}");

            async Task<JobItem> Schedule(uint index)
            {
                string? jobId = GetChildJobIdByIndex(index);
                IJobGrain grain = GrainFactory.GetGrain<IJobGrain>(jobId);
                var labels = new Dictionary<string, string>(State.Metadata.Labels)
                {
                    {"owner_id", State.Metadata.Uid },
                    {"owner_type" ,"cronjob"},
                    {"cron_index", index.ToString() }
                };
                var annotations = new Dictionary<string, string>(State.Metadata.Annotations)
                {
                };
                Job jobState = await grain.Schedule(
                    State.Spec.CommandName,
                    State.Spec.CommandData,
                    null,
                    labels,
                    annotations);
                return new JobItem(index, jobId, DateTime.UtcNow, jobState.Status.ExecutionStatus);
            }
        }

        private string GetChildJobIdByIndex(uint index) => $"cron/{State.Metadata.Uid}/{index}";

        private async Task TryComplete()
        {
            bool hasRunningJobs = State.HasRunningJobs;
            if (hasRunningJobs)
            {
                EnsureStatusProber();
                _logger.LogDebug($"CronJob[{State.Metadata.Uid}]: Can not complete since there're jobs still running, try later");
                await TickAfter(TimeSpan.FromSeconds(20));
            }

            var @event = new CronJobCompleted();
            await RaiseAsync(@event, nameof(CronJobCompleted));
            await StopTicker();
        }


        private async Task ScheduleNextTick()
        {
            DateTime now = DateTime.UtcNow;
            DateTime notBefore = now.AddSeconds(-5);
            DateTime nextTick = State.GetNextTick(notBefore) ?? now;
            await TickAfter(nextTick.Subtract(now));
        }

        private async Task TickAfter(TimeSpan dueTime)
        {
            _tickTimer?.Dispose();
            if (dueTime.TotalMinutes < 2)
            {
                _tickTimer = RegisterTimer(_ => Tick(), null, dueTime, TimeSpan.FromMilliseconds(-1));
                if (_tickReminder is null)
                {
                    _tickReminder = await RegisterOrUpdateReminder("Ticker", dueTime.Add(TimeSpan.FromMinutes(2)), TimeSpan.FromMinutes(2));
                }
            }
            else
            {
                _tickReminder = await RegisterOrUpdateReminder("Ticker", dueTime, TimeSpan.FromMinutes(2));
            }
            _logger.LogDebug($"CronJob[{State.Metadata.Uid}]: Tick After {dueTime}");
        }

        private async Task StopTicker()
        {
            _tickTimer?.Dispose();
            if (_tickReminder is null)
            {
                _tickReminder = await GetReminder("Ticker");
            }
            if (_tickReminder is not null)
            {
                await UnregisterReminder(_tickReminder);
            }
        }

        private void EnsureStatusProber()
        {
            if (_statusProber is null)
            {
                _statusProber = RegisterTimer(_ => CheckJobStatus(), null, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(20));
            }
        }
        private void StopProbeTimer() => _statusProber?.Dispose();

        Task IRemindable.ReceiveReminder(string reminderName, TickStatus status) => Tick();

        public async Task CommitOffset(long offset)
        {
            Guard.IsBetweenOrEqualTo(offset, _consumerOffset, State.Version, nameof(offset));
            await _eventStore.SaveConsumerOffset(State.Metadata.Uid, offset);
            _consumerOffset = offset;

            if (_consumingCompletionSource != null && _consumerOffset == State.Version)
            {
                _consumingCompletionSource.SetResult(true);
            }
        }

        public async Task WaitEventsConsumed()
        {
            if (ConsumerNotFollowedUp)
            {
                _consumingCompletionSource = new TaskCompletionSource<bool>();
                await _consumingCompletionSource.Task;
            }

        }
    }
}
