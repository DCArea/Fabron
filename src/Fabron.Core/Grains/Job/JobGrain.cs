﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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
        Task<ExecutionStatus> GetStatus();
        Task<JobState> Schedule(string commandName, string commandData, DateTime? schedule = null, Dictionary<string, string>? labels = null);
    }

    public class JobGrain : Grain, IJobGrain, IRemindable
    {
        private readonly ILogger _logger;
        private readonly IPersistentState<JobState> _jobState;
        private readonly IMediator _mediator;
        private IDisposable? _timer;
        private IGrainReminder? _reminder;
        private CancellationTokenSource? _cancellationTokenSource;

        public JobGrain(
            ILogger<JobGrain> logger,
            [PersistentState("Job", "JobStore")] IPersistentState<JobState> jobState,
            IMediator mediator)
        {
            _logger = logger;
            _jobState = jobState;
            _mediator = mediator;
        }

        private JobState Job => _jobState.State;

        public Task<JobState?> GetState()
        {
            JobState? state = _jobState.RecordExists ? _jobState.State : null;
            return Task.FromResult(state);
        }

        public Task<ExecutionStatus> GetStatus() => Task.FromResult(Job.Status.ExecutionStatus);

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

            Job.Status = Job.Status with
            {
                ExecutionStatus = ExecutionStatus.Canceled,
                FinishedAt = DateTime.UtcNow,
                Reason = reason,
            };
            await SaveJobStateAsync();
            MetricsHelper.JobCount_Canceled.Inc();
        }

        public async Task<JobState> Schedule(string commandName, string commandData, DateTime? schedule = null, Dictionary<string, string>? labels = null)
        {
            if (!_jobState.RecordExists)
            {
                DateTime createdAt = DateTime.UtcNow;
                DateTime schedule_ = schedule is null || schedule.Value < createdAt ? createdAt : (DateTime)schedule;
                _jobState.State = new JobState
                {
                    Metadata = new JobMetadata(this.GetPrimaryKeyString(), createdAt, labels ?? new()),
                    Spec = new JobSpec(schedule_, commandName, commandData),
                    Status = new JobStatus()
                };
                await SaveJobStateAsync();
                MetricsHelper.JobCount_Created.Inc();
                _logger.LogDebug($"Created Job");
            }

            await Next();

            return Job;
        }

        private async Task Next()
        {
            while (true)
            {
                if (Job.Status.Finalized)
                {
                    return;
                }
                var dueTime = Job.DueTime;
                if (Job.Status is { ExecutionStatus: ExecutionStatus.Scheduled } && dueTime is { TotalSeconds: >= 2 * 60 })
                {
                    return;
                }
                if (Job.Status is { ExecutionStatus: ExecutionStatus.Scheduled } && dueTime is { TotalMilliseconds: > 15 and < 2 * 60 * 1_000 })
                {
                    StartAfter(_jobState.State.DueTime);
                    return;
                }

                Task next = Job.Status switch
                {
                    { ExecutionStatus: ExecutionStatus.NotScheduled } => Schedule(),
                    { ExecutionStatus: ExecutionStatus.Scheduled } => Start(),
                    { ExecutionStatus: ExecutionStatus.Started } => Execute(),
                    { ExecutionStatus: ExecutionStatus.Succeed or ExecutionStatus.Faulted } => Cleanup(),
                    _ => throw new InvalidOperationException()
                };
                await next;
            }
        }

        private async Task Schedule()
        {
            TimeSpan dueTime = _jobState.State.DueTime;
            await CheckAfter(dueTime);

            Job.Status = Job.Status with
            {
                ExecutionStatus = ExecutionStatus.Scheduled
            };
            await SaveJobStateAsync();
            MetricsHelper.JobCount_Scheduled.Inc();
        }

        private async Task Start()
        {
            _timer?.Dispose();

            Job.Status = Job.Status with
            {
                StartedAt = DateTime.UtcNow,
                ExecutionStatus = ExecutionStatus.Started,
            };
            await SaveJobStateAsync();

            MetricsHelper.JobCount_Running.Inc();
            MetricsHelper.JobScheduleTardiness.Observe(_jobState.State.Tardiness.TotalSeconds);
        }

        private async Task Execute()
        {
            _logger.LogDebug($"Run Job");
            _cancellationTokenSource ??= new CancellationTokenSource(TimeSpan.FromMinutes(1));
            try
            {
                string? result = await _mediator.Handle(_jobState.State.Spec.CommandName, _jobState.State.Spec.CommandData, _cancellationTokenSource.Token);
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
            _logger.LogDebug($"Job Finished: {Job.Status.ExecutionStatus}");
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

            Job.Status = Job.Status with
            {
                Finalized = true
            };

            await GrainFactory.GetGrain<IJobReporterGrain>(this.GetPrimaryKeyString())
                .OnJobFinalized(_jobState.State);
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
            _timer = RegisterTimer(_ => Start(), null, dueTime, TimeSpan.FromMilliseconds(-1));
            _logger.LogDebug($"Set Job Timer with, dueTime={dueTime}");
        }

        private async Task SaveJobStateAsync()
        {
            Job.Metadata = Job.Metadata with
            {
                ResourceVersion = Job.Metadata.ResourceVersion + 1
            };
            await _jobState.WriteStateAsync();

            if (!Job.Status.Finalized)
            {
                await GrainFactory.GetGrain<IJobReporterGrain>(this.GetPrimaryKeyString())
                    .OnJobStateChanged(Job);
            }
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
