
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

        [AlwaysInterleave]
        Task Delete();

        Task Suspend();

        Task Resume();

        [AlwaysInterleave]
        Task CommitOffset(long version);
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
            string key = this.GetPrimaryKeyString();
            _consumer = GrainFactory.GetGrain<ICronJobEventConsumer>(key);
            List<EventLog> eventLogs = await _eventStore.GetEventLogs(key, 0);
            foreach (EventLog? eventLog in eventLogs)
            {
                TransitionState(eventLog);
            }

            _offset = await _eventStore.GetConsumerOffset(key);
        }

        private ICronJobEventConsumer _consumer = default!;
        private long _offset;
        private CronJob? _state;
        private CronJob State
        {
            get
            {
                Guard.IsNotNull(_state, nameof(State));
                return _state;
            }
        }

        public Task<CronJob?> GetState() => Task.FromResult(_state);

        public async Task Delete()
        {
            await StopTicker();
            // Un-Index
            await _eventStore.ClearEventLogs(State.Metadata.Uid, long.MaxValue);
            await _eventStore.ClearConsumerOffset(State.Metadata.Uid);
            _offset = -1;
            _state = null;
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
            await _eventStore.SaveConsumerOffset(State.Metadata.Uid, offset);
            _offset = offset;
        }
    }
}
