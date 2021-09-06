
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Fabron.Mando;
using Fabron.Models;
using Microsoft.Extensions.Logging;

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
        Task<Job> Schedule(string commandName, string commandData, DateTime? schedule = null, Dictionary<string, string>? labels = null);

        [AlwaysInterleave]
        Task Delete();
    }

    public class JobGrain : Grain, IJobGrain, IRemindable
    {
        private readonly ILogger _logger;
        private readonly IPersistentState<Job> _jobState;
        private readonly IMediator _mediator;
        private readonly IJobEventBus _bus;
        private IGrainReminder? _tickReminder;
        private IDisposable? _tickTimer;

        public JobGrain(
            ILogger<JobGrain> logger,
            [PersistentState("Job", "JobStore")] IPersistentState<Job> jobState,
            IMediator mediator,
            IJobEventBus bus)
        {
            _logger = logger;
            _jobState = jobState;
            _mediator = mediator;
            _bus = bus;
        }

        private Job Job => _jobState.State;

        public Task<Job?> GetState()
        {
            Job? state = _jobState.RecordExists ? _jobState.State : null;
            return Task.FromResult(state);
        }

        public Task<ExecutionStatus> GetStatus() => Task.FromResult(Job.Status.ExecutionStatus);

        public async Task Delete()
        {
            await StopTicker();
            await _jobState.ClearStateAsync();
        }

        public async Task<Job> Schedule(string commandName, string commandData, DateTime? schedule = null, Dictionary<string, string>? labels = null)
        {
            DateTime createdAt = DateTime.UtcNow;
            DateTime schedule_ = schedule is null || schedule.Value < createdAt ? createdAt : (DateTime)schedule;
            _jobState.State = new Job
            {
                Metadata = new JobMetadata(this.GetPrimaryKeyString(), createdAt, labels ?? new()),
                Spec = new JobSpec(schedule_, commandName, commandData),
                Status = new JobStatus()
            };
            TimeSpan dueTime = Job.DueTime;

            if (dueTime.TotalMinutes < 2)
            {
                _tickReminder = await RegisterOrUpdateReminder("Ticker", dueTime.Add(TimeSpan.FromMinutes(2)), TimeSpan.FromMinutes(2));
            }
            else
            {
                _tickReminder = await RegisterOrUpdateReminder("Ticker", dueTime, TimeSpan.FromMinutes(2));
            }

            await SaveJobStateAsync();
            _logger.LogDebug($"Job{Job.Metadata.Uid} Scheduled");
            MetricsHelper.JobCount_Scheduled.Inc();

            dueTime = Job.DueTime;
            if (dueTime.TotalMinutes < 2)
            {
                _tickTimer = RegisterTimer(_ => Next(), null, dueTime, TimeSpan.FromMilliseconds(-1));
            }

            return Job;
        }

        private async Task Next()
        {
            _tickTimer?.Dispose();
            if (Job.Status.Finalized)
            {
                return;
            }

            Task next = Job.Status switch
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
            _logger.LogDebug($"Job{Job.Metadata.Uid} Starting");
            Job.Status = Job.Status with
            {
                StartedAt = DateTime.UtcNow,
                ExecutionStatus = ExecutionStatus.Started,
            };
            await SaveJobStateAsync();
            MetricsHelper.JobCount_Running.Inc();
            MetricsHelper.JobScheduleTardiness.Observe(_jobState.State.Tardiness.TotalSeconds);
            _logger.LogDebug($"Job{Job.Metadata.Uid} Started");

            await Next();
        }

        private async Task Execute()
        {
            _logger.LogDebug($"Job{Job.Metadata.Uid} Executing");
            try
            {
                string? result = await _mediator.Handle(_jobState.State.Spec.CommandName, _jobState.State.Spec.CommandData);
                Job.Status = Job.Status with
                {
                    ExecutionStatus = ExecutionStatus.Succeed,
                    FinishedAt = DateTime.UtcNow,
                    Result = result,
                };
                MetricsHelper.JobCount_RanToCompletion.Inc();
            }
            catch (Exception e)
            {
                if (e is not TaskCanceledException || Job.Status.ExecutionStatus != ExecutionStatus.Canceled)
                {
                    Job.Status = Job.Status with
                    {
                        ExecutionStatus = ExecutionStatus.Faulted,
                        FinishedAt = DateTime.UtcNow,
                        Reason = e.ToString(),
                    };
                    MetricsHelper.JobCount_Faulted.Inc();
                }
            }
            finally
            {
                await SaveJobStateAsync();
            }
            _logger.LogDebug($"Job{Job.Metadata.Uid} Executing completed({Job.Status.ExecutionStatus})");

            await Next();
        }

        private async Task Cleanup()
        {
            Job.Status = Job.Status with
            {
                Finalized = true
            };
            await SaveJobStateAsync();
            await StopTicker();
            _logger.LogDebug($"Job[{Job.Metadata.Uid}]: Finalized");
            DeactivateOnIdle();
        }

        private async Task EnsureTicker() => _tickReminder = await RegisterOrUpdateReminder("Ticker", TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));

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

        private async Task SaveJobStateAsync()
        {
            Job.Version += 1;
            await _jobState.WriteStateAsync();

            await _bus.OnJobStateChanged(Job);
        }

        Task IRemindable.ReceiveReminder(string reminderName, TickStatus status)
        {
            try
            {
                return Next();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Job[{Job.Metadata.Uid}]: Error on ReceiveReminder");
                throw;
            }
        }
    }
}
