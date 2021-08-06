// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

using Fabron.Mando;

using Microsoft.Extensions.Logging;

using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Fabron.Grains.Job
{

    public interface IJobGrain : IGrainWithStringKey
    {
        [AlwaysInterleave]
        Task Cancel(string reason);
        [ReadOnly]
        Task<JobState?> GetState();
        [ReadOnly]
        Task<JobStatus> GetStatus();
        Task Schedule(JobCommandInfo command, DateTime? schedule = null);
    }

    public class JobGrain : Grain, IJobGrain, IRemindable
    {
        private readonly ILogger _logger;
        private readonly IPersistentState<JobState> _job;
        private readonly IMediator _mediator;
        private IDisposable? _timer;
        private IGrainReminder? _reminder;
        private CancellationTokenSource? _cancellationTokenSource;

        public JobGrain(
            ILogger<JobGrain> logger,
            [PersistentState("Job", "JobStore")] IPersistentState<JobState> job,
            IMediator mediator)
        {
            _logger = logger;
            _job = job;
            _mediator = mediator;
        }

        public Task<JobState?> GetState()
        {
            JobState? state = _job.RecordExists ? _job.State : null;
            return Task.FromResult(state);
        }

        public Task<JobStatus> GetStatus() => Task.FromResult(_job.State.Status);

        public async Task Cancel(string reason)
        {
            if (_cancellationTokenSource is null)
            {
                throw new InvalidOperationException();
            }

            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }

            _job.State.Status = JobStatus.Canceled;
            _job.State.FinishedAt = DateTime.UtcNow;
            _job.State.Reason = reason;
            await SaveJobStateAsync();
            MetricsHelper.JobCount_Canceled.Inc();
        }

        public async Task Schedule(JobCommandInfo command, DateTime? schedule = null)
        {
            if (!_job.RecordExists)
            {

                DateTime createdAt = DateTime.UtcNow;
                DateTime schedule_ = schedule is null || schedule.Value < createdAt ? createdAt : (DateTime)schedule;
                _job.State = new JobState
                {
                    Spec = new JobSpec(schedule_, command.Name, command.Data),
                    CreatedAt = createdAt
                };
                await SaveJobStateAsync();
                MetricsHelper.JobCount_Created.Inc();
                _logger.LogDebug($"Created Job");
            }

            await Next();
        }

        private async Task Next()
        {
            while (true)
            {
                if (_job.State.Finalized)
                {
                    return;
                }
                if (_job.State is { Status: JobStatus.Scheduled, DueTime.TotalSeconds: > 2 * 60 })
                {
                    return;
                }
                if (_job.State is { Status: JobStatus.Scheduled, DueTime.TotalSeconds: > 10 and < 2 * 60 })
                {
                    StartAfter(_job.State.DueTime);
                    return;
                }

                Task next = _job.State switch
                {
                    { Status: JobStatus.Created } => Schedule(),
                    { Status: JobStatus.Scheduled, DueTime.TotalSeconds: < 10 } => Start(),
                    { Status: JobStatus.Started } => Execute(),
                    { Status: JobStatus.Succeed or JobStatus.Canceled or JobStatus.Faulted } => Cleanup(),
                    _ => throw new InvalidOperationException()
                };
                await next;
            }
        }

        private async Task Schedule()
        {
            TimeSpan dueTime = _job.State.DueTime;
            await CheckAfter(dueTime);

            _job.State.Status = JobStatus.Scheduled;
            await SaveJobStateAsync();
            MetricsHelper.JobCount_Scheduled.Inc();
        }

        private async Task Start()
        {
            _timer?.Dispose();

            _job.State.StartedAt = DateTime.UtcNow;
            _job.State.Status = JobStatus.Started;
            await SaveJobStateAsync();

            MetricsHelper.JobCount_Running.Inc();
            MetricsHelper.JobScheduleTardiness.Observe(_job.State.Tardiness.TotalSeconds);
        }

        private async Task Execute()
        {
            _logger.LogDebug($"Run Job");
            _cancellationTokenSource ??= new CancellationTokenSource(TimeSpan.FromMinutes(1));
            try
            {
                string? result = await _mediator.Handle(_job.State.Spec.CommandName, _job.State.Spec.CommandData, _cancellationTokenSource.Token);
                _job.State.Status = JobStatus.Succeed;
                _job.State.FinishedAt = DateTime.UtcNow;
                _job.State.Result = result;
                MetricsHelper.JobCount_RanToCompletion.Inc();
            }
            catch (Exception e)
            {
                if (e is not TaskCanceledException || _job.State.Status != JobStatus.Canceled)
                {
                    _job.State.Status = JobStatus.Faulted;
                    _job.State.FinishedAt = DateTime.UtcNow;
                    _job.State.Reason = e.ToString();
                    MetricsHelper.JobCount_Faulted.Inc();
                }
            }
            finally
            {
                await SaveJobStateAsync();
            }
            _logger.LogDebug($"Job Finished: {_job.State.Status}");
        }

        private async Task Cleanup()
        {
            if (_reminder is null)
            {
                _reminder = await GetReminder("Check");
            }

            if (_reminder is not null)
            {
                await UnregisterReminder(_reminder);
                _reminder = null;
                _logger.LogDebug($"Job Reminder Unregistered");
            }

            _job.State.Finalized = true;
            await SaveJobStateAsync();
            DeactivateOnIdle();
        }

        private async Task CheckAfter(TimeSpan dueTime)
        {
            if (dueTime < TimeSpan.FromMinutes(2))
            {
                await SetReminder(TimeSpan.FromMinutes(2));
            }
            else
            {
                await SetReminder(dueTime - TimeSpan.FromMinutes(2));
            }

            async Task SetReminder(TimeSpan dueTime)
            {
                _reminder = await RegisterOrUpdateReminder("Check", dueTime, TimeSpan.FromMinutes(2));
                _logger.LogDebug($"Job Reminder Registered, dueTime={dueTime}");
            }
        }
        private void StartAfter(TimeSpan dueTime)
        {
            _timer?.Dispose();
            _timer = RegisterTimer(_ => Start(), null, dueTime, TimeSpan.MaxValue);
            _logger.LogDebug($"Set Job Timer with, dueTime={dueTime}");
        }

        private async Task SaveJobStateAsync()
        {
            await _job.WriteStateAsync();

            _ = long.TryParse(_job.Etag, out long version);
            await GrainFactory.GetGrain<IJobReporterGrain>(this.GetPrimaryKeyString())
                .OnJobStateChanged(version, _job.State);
        }

        Task IRemindable.ReceiveReminder(string reminderName, TickStatus status)
        {
            try
            {
                return Next();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error on ReceiveReminder");
                throw;
            }
        }
    }
}
