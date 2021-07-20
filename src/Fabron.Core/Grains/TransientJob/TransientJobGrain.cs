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
using Prometheus;

namespace Fabron.Grains.TransientJob
{

    public interface ITransientJobGrain : IGrainWithStringKey
    {
        [AlwaysInterleave]
        Task Cancel(string reason);
        [ReadOnly]
        Task<TransientJobState?> GetState();
        [ReadOnly]
        Task<JobStatus> GetStatus();
        Task Create(JobCommandInfo command, DateTime? scheduledAt = null);
    }

    public class TransientJobGrain : Grain, ITransientJobGrain, IRemindable
    {
        private static readonly Counter ProcessedJobCount = Metrics
            .CreateCounter("fabron_jobs_processed_total", "Number of processed jobs.");
        private static readonly Counter ReceivedJobCount = Metrics
            .CreateCounter("fabron_jobs_received_total", "Number of received jobs.");

        private readonly ILogger _logger;
        private readonly IPersistentState<TransientJobState> _job;
        private readonly IMediator _mediator;
        private IDisposable? _timer;
        private IGrainReminder? _reminder;
        private CancellationTokenSource? _cancellationTokenSource;

        public TransientJobGrain(
            ILogger<TransientJobGrain> logger,
            [PersistentState("Job", "JobStore")] IPersistentState<TransientJobState> job,
            IMediator mediator)
        {
            _logger = logger;
            _job = job;
            _mediator = mediator;
        }

        public Task<TransientJobState?> GetState()
        {
            TransientJobState? state = _job.RecordExists ? _job.State : null;
            return Task.FromResult(state);
        }

        public Task<JobStatus> GetStatus() => Task.FromResult(_job.State.Status);

        public async Task Create(JobCommandInfo command, DateTime? scheduledAt = null)
        {
            if (!_job.RecordExists)
            {
                _job.State = new TransientJobState(command, scheduledAt);
                await _job.WriteStateAsync();
                _logger.LogInformation($"Created Job");
            }

            await Schedule();
        }

        private async Task Schedule()
        {
            ReceivedJobCount.Inc();
            TimeSpan dueTime = _job.State.DueTime;
            _logger.LogInformation($"Schedule Job, dueTime={dueTime}");
            switch (dueTime)
            {
                case TimeSpan when dueTime == TimeSpan.Zero:
                    await SetJobReminder(TimeSpan.FromMinutes(2));
                    _ = Start();
                    break;
                case TimeSpan when dueTime < TimeSpan.FromMinutes(1):
                    SetJobTimer(dueTime);
                    await SetJobReminder(TimeSpan.FromMinutes(2));
                    break;
                default:
                    await SetJobReminder(dueTime);
                    break;
            }
        }

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
            ProcessedJobCount.WithLabels(nameof(JobStatus.Canceled)).Inc();
            await _job.WriteStateAsync();
        }

        private async Task Start()
        {
            _timer?.Dispose();
            _logger.LogInformation($"Start Job");
            _job.State.Start();
            await _job.WriteStateAsync();
            _logger.LogInformation($"Job Started");
            await Run();
        }


        private async Task Run()
        {
            _logger.LogInformation($"Run Job");
            _cancellationTokenSource ??= new CancellationTokenSource(TimeSpan.FromMinutes(10));
            try
            {
                string? result = await _mediator.Handle(_job.State.Command.Name, _job.State.Command.Data, _cancellationTokenSource.Token);
                _job.State.Complete(result);
                ProcessedJobCount.WithLabels(nameof(JobStatus.RanToCompletion)).Inc();
            }
            catch (Exception e)
            {
                if (e is not TaskCanceledException || _job.State.Status != JobStatus.Canceled)
                {
                    _job.State.Fault(e);
                    ProcessedJobCount.WithLabels(nameof(JobStatus.Faulted)).Inc();
                }
            }
            await _job.WriteStateAsync();
            _logger.LogInformation($"Job Finished: {_job.State.Status}");
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

        private Task Go() => _job.State.Status switch
        {
            JobStatus.NotCreated or JobStatus.Created => Start(),
            JobStatus.Running => Run(),
            _ => Cleanup()
        };

        private void SetJobTimer(TimeSpan dueTime)
        {
            _timer = RegisterTimer(_ => Start(), null, dueTime, TimeSpan.MaxValue);
            _logger.LogInformation($"Set Job Timer with, dueTime={dueTime}");
        }
        private async Task SetJobReminder(TimeSpan dueTime)
        {
            _reminder = await RegisterOrUpdateReminder("Check", dueTime, TimeSpan.FromMinutes(2));
            _logger.LogInformation($"Job Reminder Registered, dueTime={dueTime}");
        }
        Task IRemindable.ReceiveReminder(string reminderName, TickStatus status) => Go();
    }
}
