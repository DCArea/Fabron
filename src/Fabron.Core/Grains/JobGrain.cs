
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
            DateTime? schedule = null,
            Dictionary<string, string>? labels = null,
            Dictionary<string, string>? annotations = null);

        [AlwaysInterleave]
        Task Delete();

        [AlwaysInterleave]
        Task CommitOffset(long version);
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
            string key = this.GetPrimaryKeyString();
            _consumer = GrainFactory.GetGrain<IJobEventConsumer>(key);
            List<EventLog> eventLogs = await _eventStore.GetEventLogs(key, 0);
            foreach (EventLog? eventLog in eventLogs)
            {
                TransitionState(eventLog);
            }

            _offset = await _eventStore.GetConsumerOffset(key);
        }

        private IJobEventConsumer _consumer = default!;
        private long _offset;
        private Job? _state;
        private Job State
        {
            get
            {
                Guard.IsNotNull(_state, nameof(State));
                return _state;
            }
        }

        public Task<Job?> GetState() => Task.FromResult(_state);

        public Task<ExecutionStatus> GetStatus() => Task.FromResult(State.Status.ExecutionStatus);

        public async Task Delete()
        {
            await StopTicker();
            // Un-Index
            await _eventStore.ClearEventLogs(State.Metadata.Uid, long.MaxValue);
            await _eventStore.ClearConsumerOffset(State.Metadata.Uid);
            _offset = -1;
            _state = null;
        }

        public async Task<Job> Schedule(
            string commandName,
            string commandData,
            DateTime? schedule = null,
            Dictionary<string, string>? labels = null,
            Dictionary<string, string>? annotations = null)
        {
            DateTime utcNow = DateTime.UtcNow;
            DateTime schedule_ = schedule is null || schedule.Value < utcNow ? utcNow : (DateTime)schedule;
            TimeSpan dueTime = schedule_ <= utcNow ? TimeSpan.Zero : schedule_ - utcNow;
            if (dueTime.TotalMinutes < 2)
            {
                _tickReminder = await RegisterOrUpdateReminder("Ticker", dueTime.Add(TimeSpan.FromMinutes(2)), TimeSpan.FromMinutes(2));
            }
            else
            {
                _tickReminder = await RegisterOrUpdateReminder("Ticker", dueTime, TimeSpan.FromMinutes(2));
            }

            JobScheduled jobScheduled = new JobScheduled(
                labels ?? new Dictionary<string, string>(),
                annotations ?? new Dictionary<string, string>(),
                schedule_,
                commandName,
                commandData
            );
            await RaiseAsync(jobScheduled);

            utcNow = DateTime.UtcNow;
            dueTime = schedule_ <= utcNow ? TimeSpan.Zero : schedule_ - utcNow;
            if (dueTime.TotalMinutes < 2)
            {
                _tickTimer = RegisterTimer(_ => Next(), null, dueTime, TimeSpan.FromMilliseconds(-1));
            }

            return State;
        }

        private async Task Next()
        {
            if (State is null)
            {
                _logger.LogError($"Broken state on Job[{this.GetPrimaryKeyString()}]");
                return;
            }

            _tickTimer?.Dispose();
            if (State.Status.Finalized)
            {
                return;
            }

            Task next = State.Status switch
            {
                { ExecutionStatus: ExecutionStatus.Scheduled } => Start(),
                { ExecutionStatus: ExecutionStatus.Started } => Execute(),
                { ExecutionStatus: ExecutionStatus.Succeed or ExecutionStatus.Faulted } => Cleanup(),
                _ => throw new InvalidOperationException()
            };
            await next;
        }

        private async Task Start()
        {
            _logger.StartingJobExecution(State.Metadata.Uid);
            MetricsHelper.JobScheduleTardiness.Observe(State.Tardiness.TotalSeconds);


            JobExecutionStarted jobExecutionStarted = new JobExecutionStarted();
            await RaiseAsync(jobExecutionStarted);

            await Next();
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

            await Next();
        }

        private async Task Cleanup()
        {
            if (_offset == State.Version)
            {
                await StopTicker();
                _logger.JobFinalized(State.Metadata.Uid);
            }
            else
            {
                await _consumer.NotifyChanged(_offset, State.Version);
            }
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

        Task IRemindable.ReceiveReminder(string reminderName, TickStatus status) => Next();

        public async Task CommitOffset(long offset)
        {
            await _eventStore.SaveConsumerOffset(State.Metadata.Uid, offset);
            _offset = offset;
        }
    }
}
