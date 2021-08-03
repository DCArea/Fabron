// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Fabron.Grains.TransientJob;
using Fabron.Mando;

using Microsoft.Extensions.Logging;

using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Fabron.Grains.BatchJob
{
    public interface IBatchJobGrain : IGrainWithStringKey
    {
        [AlwaysInterleave]
        Task Cancel(string reason);
        [ReadOnly]
        Task<BatchJobState> GetState();
        Task Create(List<JobCommandInfo> commands);
    }

    public class BatchJobGrain : Grain, IBatchJobGrain, IRemindable
    {
        private readonly ILogger _logger;
        private readonly IPersistentState<BatchJobState> _job;
        private IGrainReminder? _reminder;
        private CancellationTokenSource? _cancellationTokenSource;

        public BatchJobGrain(
            ILogger<JobGrain> logger,
            [PersistentState("BatchJob", "JobStore")] IPersistentState<BatchJobState> job,
            IMediator mediator)
        {
            _logger = logger;
            _job = job;
        }

        public Task<BatchJobState> GetState() => Task.FromResult(_job.State);

        public async Task Create(List<JobCommandInfo> commands)
        {
            if (!_job.RecordExists)
            {
                _job.State = new(commands);
                await _job.WriteStateAsync();
                _logger.LogInformation($"Created Job");
            }

            _reminder = await RegisterOrUpdateReminder("Check", TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(10));
            _logger.LogInformation($"Job Reminder Registered");
            _ = Go();
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
            await _job.WriteStateAsync();
        }

        private async Task Start()
        {
            _logger.LogInformation($"Start Job");
            _job.State.Start();
            await _job.WriteStateAsync();
            _logger.LogInformation($"Job Started");
            await Run();
        }


        private async Task Run()
        {
            _logger.LogInformation($"Run BatchJob");
            _cancellationTokenSource ??= new CancellationTokenSource(TimeSpan.FromMinutes(30));

            await Task.WhenAll(EnqueueChildJobs(), CheckChildJobs());

            _job.State.Complete();
            _logger.LogInformation($"BatchJob Finished: {_job.State.Status}");
            await Cleanup();
        }

        private async Task EnqueueChildJobs()
        {
            while (true)
            {
                List<BatchJobStateChild> pendingJobs = _job.State.PendingJobs.Take(10).ToList();
                if (pendingJobs.Count == 0)
                {
                    return;
                }

                IEnumerable<Task> enqueueChildJobTasks = pendingJobs
                    .Select(job => CreateChildJob(job));
                IEnumerable<Task> checkJobStatusTasks = pendingJobs
                    .Select(job => CheckChildJobStatus(job));

                await Task.WhenAll(enqueueChildJobTasks);
                await Task.WhenAll(checkJobStatusTasks);
                await _job.WriteStateAsync();
            }
        }

        private async Task CheckChildJobs()
        {
            while (true)
            {
                List<BatchJobStateChild> jobsToCheck = _job.State.ScheduledJobs.ToList();
                if (jobsToCheck.Count == 0)
                {
                    return;
                }

                IEnumerable<Task> checkJobStatusTasks = jobsToCheck
                    .Select(job => CheckChildJobStatus(job));

                await Task.WhenAll(checkJobStatusTasks);
                await _job.WriteStateAsync();
            }
        }


        private async Task CreateChildJob(BatchJobStateChild job)
        {
            IJobGrain grain = GrainFactory.GetGrain<IJobGrain>(job.Id);
            await grain.Schedule(job.Command);
            job.Status = ChildJobStatus.WaitToSchedule;
        }

        private async Task CheckChildJobStatus(BatchJobStateChild job)
        {
            IJobGrain grain = GrainFactory.GetGrain<IJobGrain>(job.Id);
            job.Status = await grain.GetStatus() switch
            {
                JobStatus.Created => ChildJobStatus.WaitToSchedule,
                JobStatus.Scheduled or JobStatus.Started => ChildJobStatus.Scheduled,
                JobStatus.Succeed => ChildJobStatus.RanToCompletion,
                JobStatus.Canceled => ChildJobStatus.Canceled,
                JobStatus.Faulted => ChildJobStatus.Faulted,
                _ => throw new InvalidOperationException("invalid child job state")
            };
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
                _logger.LogInformation($"Job Reminder Unregistered");
            }
            DeactivateOnIdle();
        }

        private Task Go() => _job.State.Status switch
        {
            BatchJobStatus.Created => Start(),
            BatchJobStatus.Running => Run(),
            _ => Cleanup()
        };

        Task IRemindable.ReceiveReminder(string reminderName, TickStatus status) => Go();
    }
}
