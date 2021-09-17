
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
using static Fabron.FabronConstants;

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

        Task Suspend();

        Task Resume();

        Task Complete();

        Task Delete();

        [AlwaysInterleave]
        Task CommitOffset(long version);

        Task Purge();

        [ReadOnly]
        Task WaitEventsConsumed(int waitSeconds);
    }

    public partial class CronJobGrain : Grain, ICronJobGrain, IRemindable
    {
        private readonly ILogger _logger;
        private readonly ICronJobEventStore _eventStore;
        private readonly IJobQuerier _querier;
        public CronJobGrain(
            ILogger<CronJobGrain> logger,
            ICronJobEventStore eventStore,
            IJobQuerier querier)
        {
            _logger = logger;
            _eventStore = eventStore;
            _querier = querier;
        }

        public override async Task OnActivateAsync()
        {
            _key = this.GetPrimaryKeyString();
            _consumer = GrainFactory.GetGrain<ICronJobEventConsumer>(_key);
            _scheduler = GrainFactory.GetGrain<ICronJobScheduler>(_key);

            var snapshot = await _querier.GetCronJobByKey(_key);
            if (snapshot is not null)
            {
                _state = snapshot;
                _logger.StateSnapshotLoaded(_key, _state.Version);
            }
            var from = _state is null ? 0L : _state.Version + 1;

            _logger.LoadingEvents(_key, from);
            List<EventLog> eventLogs = await _eventStore.GetEventLogs(_key, from);
            foreach (EventLog? eventLog in eventLogs)
            {
                TransitionState(eventLog);
            }

            _consumerOffset = await _eventStore.GetConsumerOffset(_key);
            _logger.ConsumerOffsetLoaded(_key, _consumerOffset);
        }

        private string _key = default!;
        private ICronJobEventConsumer _consumer = default!;
        private ICronJobScheduler _scheduler = default!;
        private long _consumerOffset;
        private CronJob? _state;
        private TaskCompletionSource<bool>? _consumingCompletionSource;
        private object _lock = new object();

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
        private bool Purged => _state is null && _consumerOffset == -1;

        public Task<CronJob?> GetState() => Task.FromResult(_state);

        public async Task Purge()
        {
            if (ConsumerNotFollowedUp)
            {
                await NotifyConsumer();
                await WaitEventsConsumed();
                return;
            }

            if (_state != null)
            {
                await _eventStore.ClearEventLogs(_key, long.MaxValue);
                _state = null;
            }
            if (_consumerOffset != -1)
            {
                await _eventStore.ClearConsumerOffset(_key);
                await _consumer.Reset();
                _consumerOffset = -1;
            }
            _logger.Purged(_key);
        }

        public async Task Delete()
        {
            if (Deleted)
            {
                return;
            }
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

        public async Task Trigger() => await _scheduler.Trigger();

        public async Task Suspend()
        {
            if (State.Spec.Suspend)
            {
                return;
            }
            var @event = new CronJobSuspended();
            await RaiseAsync(@event, nameof(CronJobSuspended));
        }

        public async Task Resume()
        {
            var state = State;
            if (state.Status.CompletionTimestamp.HasValue)
            {
                ThrowHelper.ThrowStartCompletedCronJob(_key);
                return;
            }

            await SetHealthCheck();
            if (state.Spec.Suspend)
            {
                var @event = new CronJobResumed();
                await RaiseAsync(@event, nameof(CronJobResumed));
            }
            await _scheduler.Start();
        }

        public async Task Complete()
        {
            //
            var state = State;
            DateTime now = DateTime.UtcNow;
            Cronos.CronExpression cron = Cronos.CronExpression.Parse(state.Spec.Schedule);
            var tick = cron.GetNextOccurrence(now.AddSeconds(-5));
            // Completed
            if (tick is null || (state.Spec.ExpirationTime.HasValue && tick.Value > state.Spec.ExpirationTime.Value))
            {
                var @event = new CronJobCompleted();
                await RaiseAsync(@event, nameof(CronJobCompleted));
            }
        }

        public async Task CommitOffset(long offset)
        {
            Guard.IsBetweenOrEqualTo(offset, _consumerOffset, State.Version, nameof(offset));
            await _eventStore.SaveConsumerOffset(State.Metadata.Key, offset);
            _consumerOffset = offset;
            _logger.ConsumerOffsetUpdated(_key, _consumerOffset);

            if (_consumingCompletionSource != null && _consumerOffset == State.Version)
            {
                _consumingCompletionSource.SetResult(true);
            }
        }

        public async Task WaitEventsConsumed(int waitSeconds = 5)
        {
            if (ConsumerNotFollowedUp)
            {
                _consumingCompletionSource = new TaskCompletionSource<bool>();
                int retry = 0;
                while (true)
                {
                    try
                    {
                        await _consumingCompletionSource.Task.WaitAsync(TimeSpan.FromMilliseconds(100));
                    }
                    catch (TimeoutException)
                    {
                        if (retry++ > 10 * waitSeconds)
                        {
                            throw;
                        }
                        await NotifyConsumer();
                    }
                }
            }

        }

        private IDisposable? _hcTimer = null;
        private IGrainReminder? _hcReminder = null;
        private async Task SetHealthCheck()
        {
            _hcReminder = await RegisterOrUpdateReminder("HealthCheck", TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
            SetHealthCheckTimer();
        }
        private void SetHealthCheckTimer()
        {
            if (_hcTimer is null)
            {
                _hcTimer = RegisterTimer(_ => HealthCheck(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            }
        }

        private async Task HealthCheck()
        {
            bool isHealthy = false;
            if (_state is null || _state.Status.Deleted || _state.Spec.Suspend)
            {
                isHealthy = true;
            }
            else
            {
                if (await _scheduler.IsRunning())
                {
                    isHealthy = true;
                }
            }

            if (isHealthy)
            {
                await StopHealthCheck();

            }
            else
            {
                _logger.CronJobSchedulerUnhealthy(_key);
                await _scheduler.Start();
            }

        }

        private async Task StopHealthCheck()
        {

            var reminder = _hcReminder ?? await GetReminder("HealthCheck");
            int retry = 0;
            while (true)
            {
                if (reminder is null) return;
                try
                {
                    await UnregisterReminder(reminder);
                    break;
                }
                catch (Orleans.Runtime.ReminderException)
                {
                    if (retry++ < 3)
                    {
                        reminder = await GetReminder("HealthCheck");
                        continue;
                    }
                    throw;
                }
            }
            _hcReminder = null;
            _hcTimer?.Dispose();
        }

        public Task ReceiveReminder(string reminderName, TickStatus status)
        {
            SetHealthCheckTimer();
            return Task.CompletedTask;
        }
    }
}
