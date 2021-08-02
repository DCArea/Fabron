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

namespace Fabron.Grains.TransientJob
{

    public interface IJobGrain : IGrainWithStringKey
    {
        [AlwaysInterleave]
        Task Cancel(string reason);
        [ReadOnly]
        Task<JobState?> GetState();
        [ReadOnly]
        Task<JobStatus> GetStatus();
        Task Schedule(JobCommandInfo command, DateTime? scheduledAt = null);
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

            _job.State.Cancel(reason);
            await _job.WriteStateAsync();
            MetricsHelper.JobCount_Canceled.Inc();
        }

        public async Task Schedule(JobCommandInfo command, DateTime? scheduledAt = null)
        {
            if (!_job.RecordExists)
            {
                _job.State = new JobState(command, scheduledAt);
                await _job.WriteStateAsync();
                MetricsHelper.JobCount_Created.Inc();
                _logger.LogDebug($"Created Job");
            }

            await Next();
        }

        private async Task Schedule()
        {
            TimeSpan dueTime = _job.State.DueTime;

            if (dueTime < TimeSpan.FromMinutes(2))
            {
                await CheckAfter(dueTime);
            }
            else
            {
                await CheckAfter(dueTime - TimeSpan.FromMinutes(2));
            }

            _job.State.Status = JobStatus.Scheduled;
            await _job.WriteStateAsync();
            MetricsHelper.JobCount_Scheduled.Inc();

            await Start();
        }

        private async Task Start()
        {
            TimeSpan dueTime = _job.State.DueTime;
            bool startImmediately = dueTime < TimeSpan.FromSeconds(10);
            if (!startImmediately)
            {
                if (dueTime < TimeSpan.FromMinutes(2))
                {
                    StartAfter(dueTime);
                }
                return;
            }

            _timer?.Dispose();
            _job.State.Start();

            await _job.WriteStateAsync();

            MetricsHelper.JobCount_Running.Inc();
            MetricsHelper.JobScheduleTardiness.Observe(_job.State.Tardiness.TotalSeconds);
            await Run();
        }


        private async Task Run()
        {
            _logger.LogInformation($"Run Job");
            _cancellationTokenSource ??= new CancellationTokenSource(TimeSpan.FromMinutes(1));
            try
            {
                string? result = await _mediator.Handle(_job.State.Command.Name, _job.State.Command.Data, _cancellationTokenSource.Token);
                _job.State.Complete(result);
                await _job.WriteStateAsync();
                MetricsHelper.JobCount_RanToCompletion.Inc();
            }
            catch (Exception e)
            {
                if (e is not TaskCanceledException || _job.State.Status != JobStatus.Canceled)
                {
                    _job.State.Fault(e);
                    await _job.WriteStateAsync();
                    MetricsHelper.JobCount_Faulted.Inc();
                }
            }
            _logger.LogDebug($"Job Finished: {_job.State.Status}");

            await Cleanup();
        }

        private async Task Cleanup()
        {
            _logger.LogInformation($"Cleanup Job");
            if (_reminder is null)
            {
                _reminder = await GetReminder("Check");
            }

            if (_reminder is not null)
            {
                await UnregisterReminder(_reminder);
                _reminder = null;
                _logger.LogInformation($"Job Reminder Unregistered");
            }
            DeactivateOnIdle();
        }

        private Task Next() => _job.State.Status switch
        {
            JobStatus.Created => Schedule(),
            JobStatus.Scheduled => Start(),
            JobStatus.Running => Run(),
            _ => Cleanup()
        };

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
