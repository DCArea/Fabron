
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron.Events;
using Fabron.Mando;
using Fabron.Models;
using Fabron.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Diagnostics;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Fabron.Grains
{
    public interface IJobGrain : IGrainWithStringKey
    {
        [ReadOnly]
        Task<Job?> GetState();

        [ReadOnly]
        Task<ExecutionStatus> GetStatus();
        Task<Job> Schedule(
            string commandName,
            string commandData,
            DateTime? schedule,
            Dictionary<string, string>? labels,
            Dictionary<string, string>? annotations);

        [AlwaysInterleave]
        Task Delete();

        [AlwaysInterleave]
        Task CommitOffset(long version);

        [AlwaysInterleave]
        Task Purge();

        [AlwaysInterleave]
        Task WaitEventsConsumed(int timeoutSeconds);
    }

    public partial class JobGrain : Grain, IJobGrain, IRemindable
    {
        private readonly ILogger _logger;
        private readonly IMediator _mediator;
        private readonly IJobEventStore _eventStore;
        private IGrainReminder? _tickReminder;
        private IDisposable? _tickTimer;

        public JobGrain(
            ILogger<JobGrain> logger,
            IMediator mediator,
            IJobEventStore store)
        {
            _logger = logger;
            _mediator = mediator;
            _eventStore = store;
        }

        public override async Task OnActivateAsync()
        {
            _key = this.GetPrimaryKeyString();
            _consumer = GrainFactory.GetGrain<IJobEventConsumer>(_key);

            List<EventLog> eventLogs = await _eventStore.GetEventLogs(_key, 0);
            foreach (EventLog? eventLog in eventLogs)
            {
                TransitionState(eventLog);
            }

            _consumerOffset = await _eventStore.GetConsumerOffset(_key);
        }

        private string _key = default!;
        private IJobEventConsumer _consumer = default!;
        private long _consumerOffset;
        private Job? _state;
        private TaskCompletionSource<bool>? _consumingCompletionSource;

        private Job State
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

        public Task<Job?> GetState() => Task.FromResult(_state);

        public Task<ExecutionStatus> GetStatus() => Task.FromResult(State.Status.ExecutionStatus);

        public async Task Purge()
        {
            if (ConsumerNotFollowedUp)
            {
                await NotifyConsumer();
                await WaitEventsConsumed(10);
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
            await StopTicker();
            _logger.JobPurged(_key);
        }

        public async Task Delete()
        {
            if (Deleted)
            {
                return;
            }
            JobDeleted? @event = new JobDeleted();
            await RaiseAsync(@event, nameof(JobDeleted));
        }

        private async Task Complete() => await StopTicker();

        public async Task<Job> Schedule(
            string commandName,
            string commandData,
            DateTime? schedule,
            Dictionary<string, string>? labels,
            Dictionary<string, string>? annotations)
        {
            DateTime utcNow = DateTime.UtcNow;
            DateTime schedule_ = schedule is null || schedule.Value < utcNow ? utcNow : (DateTime)schedule;
            await EnsureTicker(TimeSpan.FromMinutes(2));

            JobScheduled jobScheduled = new JobScheduled(
                labels ?? new Dictionary<string, string>(),
                annotations ?? new Dictionary<string, string>(),
                schedule_,
                commandName,
                commandData
            );
            await RaiseAsync(jobScheduled);

            utcNow = DateTime.UtcNow;
            TimeSpan dueTime = schedule_ <= utcNow ? TimeSpan.Zero : schedule_ - utcNow;
            await TickAfter(dueTime);

            return State;
        }

        private async Task Tick()
        {
            if (DeletedButNotPurged)
            {
                await Purge();
                return;
            }

            _tickTimer?.Dispose();
            Task next = State.Status switch
            {
                { ExecutionStatus: ExecutionStatus.Scheduled } => Start(),
                { ExecutionStatus: ExecutionStatus.Started } => Execute(),
                { ExecutionStatus: ExecutionStatus.Succeed or ExecutionStatus.Faulted } => Complete(),
                _ => throw new InvalidOperationException()
            };
            await next;
        }

        private async Task Start()
        {
            _logger.StartingJobExecution(State.Metadata.Key);
            MetricsHelper.JobScheduleTardiness.Observe(State.Tardiness.TotalSeconds);


            JobExecutionStarted jobExecutionStarted = new JobExecutionStarted();
            await RaiseAsync(jobExecutionStarted);

            await Tick();
        }

        private async Task Execute()
        {
            try
            {
                string? result = await _mediator.Handle(State.Spec.CommandName, State.Spec.CommandData);
                Guard.IsNotNull(result, nameof(result));
                JobExecutionSucceed jobExecutionSucceed = new JobExecutionSucceed(result);
                await RaiseAsync(jobExecutionSucceed);
                MetricsHelper.JobCount_RanToCompletion.Inc();
            }
            catch (Exception e)
            {
                if (e is not TaskCanceledException || State.Status.ExecutionStatus != ExecutionStatus.Canceled)
                {
                    JobExecutionFailed jobExecutionFailed = new JobExecutionFailed(e.Message);
                    await RaiseAsync(jobExecutionFailed);
                }
            }

            await Tick();
        }

        private async Task TickAfter(TimeSpan dueTime)
        {
            _tickTimer?.Dispose();
            if (dueTime.TotalMinutes < 2)
            {
                _tickTimer = RegisterTimer(_ => Tick(), null, dueTime, TimeSpan.FromMilliseconds(-1));
                if (_tickReminder is null)
                {
                    await EnsureTicker(dueTime.Add(TimeSpan.FromMinutes(2)));
                }
            }
            else
            {
                await EnsureTicker(dueTime);
            }
            _logger.LogDebug($"Job[{_key}]: Tick After {dueTime}");
        }

        private async Task EnsureTicker(TimeSpan dueTime) => _tickReminder = await RegisterOrUpdateReminder("Ticker", dueTime, TimeSpan.FromMinutes(2));

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

        Task IRemindable.ReceiveReminder(string reminderName, TickStatus status) => Tick();

        public async Task CommitOffset(long offset)
        {
            Guard.IsBetweenOrEqualTo(offset, _consumerOffset, State.Version, nameof(offset));
            await _eventStore.SaveConsumerOffset(State.Metadata.Key, offset);
            _consumerOffset = offset;

            if (_consumingCompletionSource != null && _consumerOffset == State.Version)
            {
                _consumingCompletionSource.SetResult(true);
            }
        }

        public async Task WaitEventsConsumed(int timeoutSeconds)
        {
            if (ConsumerNotFollowedUp)
            {
                _consumingCompletionSource = new TaskCompletionSource<bool>();
                try
                {
                    await _consumingCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(timeoutSeconds));
                }
                catch (TimeoutException)
                {
                }
                if (ConsumerNotFollowedUp)
                {
                    ThrowHelper.ThrowConsumerNotFollowedUp(_key, State.Version, _consumerOffset);
                }
            }
        }
    }
}
