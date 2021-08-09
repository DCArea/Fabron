// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Fabron.Grains.Job;
using Fabron.Mando;

using Microsoft.Extensions.Logging;

using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Fabron.Grains.CronJob
{
    public interface ICronJobGrain : IGrainWithStringKey
    {
        [AlwaysInterleave]
        Task Cancel(string reason);
        [ReadOnly]
        Task<CronJobState> GetState();
        Task Create(string cronExp, JobCommandInfo commands);
    }

    public class CronJobGrain : Grain, ICronJobGrain, IRemindable
    {
        private readonly ILogger _logger;
        private readonly IPersistentState<CronJobState> _job;
        private IGrainReminder? _reminder;
        private CancellationTokenSource? _cancellationTokenSource;

        public CronJobGrain(
            ILogger<JobGrain> logger,
            [PersistentState("CronJob", "JobStore")] IPersistentState<CronJobState> job,
            IMediator mediator)
        {
            _logger = logger;
            _job = job;
        }

        public Task<CronJobState> GetState() => Task.FromResult(_job.State);

        public async Task Create(string cronExp, JobCommandInfo commands)
        {
            if (!_job.RecordExists)
            {
                _job.State = new(cronExp, commands);
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
            _logger.LogInformation($"Run CronJob");
            _cancellationTokenSource ??= new CancellationTokenSource();

            await Task.WhenAll(ScheduleChildJobs(), CheckPendingJobs());


            CronJobStateChild? firstUnfinishedJob = _job.State.ChildJobs.Where(cj => !cj.IsFinished).FirstOrDefault();
            if (firstUnfinishedJob is null)
            {
                _job.State.Complete();
                _logger.LogInformation($"CronJob Finished: {_job.State.Status}");
                await Cleanup();
            }
            else
            {
                DateTime now = DateTime.UtcNow;
                DateTime after5Min = DateTime.UtcNow.AddMinutes(5);
                DateTime nextSchedule = firstUnfinishedJob.ScheduledAt;
                if (firstUnfinishedJob.ScheduledAt < after5Min)
                {
                    await SetJobReminder(TimeSpan.FromMinutes(5));
                }
                else
                {
                    await SetJobReminder(nextSchedule - now);
                }
            }
        }

        private async Task ScheduleChildJobs()
        {
            DateTime utcNow = DateTime.UtcNow;
            DateTime toTime = utcNow.AddMinutes(20);
            _job.State.Schedule(toTime);
            List<CronJobStateChild> jobsToBeScheduled = _job.State.PendingJobs.ToList();

            if (jobsToBeScheduled.Count == 0)
            {
                // TODO: check when to schedule the next child job
                return;
            }

            IEnumerable<Task> enqueueChildJobTasks = jobsToBeScheduled
                .Select(job => CreateChildJob(job));
            await Task.WhenAll(enqueueChildJobTasks);
            //IEnumerable<Task> checkJobStatusTasks = jobsToBeScheduled
            //    .Select(job => CheckChildJobStatus(job));
            //await Task.WhenAll(checkJobStatusTasks);
            await _job.WriteStateAsync();
        }

        // private async Task Check

        private async Task CheckPendingJobs()
        {
            DateTime utcNow = DateTime.UtcNow;
            List<CronJobStateChild> jobsToBeChecked = _job.State.ScheduledJobs.Where(job => job.ScheduledAt < utcNow).ToList();
            if (jobsToBeChecked.Count == 0)
            {
                return;
            }

            IEnumerable<Task> checkJobStatusTasks = jobsToBeChecked
                .Select(job => CheckChildJobStatus(job));

            await Task.WhenAll(checkJobStatusTasks);
            await _job.WriteStateAsync();
        }

        private async Task CreateChildJob(CronJobStateChild job)
        {
            IJobGrain grain = GrainFactory.GetGrain<IJobGrain>(job.Id);
            await grain.Schedule(_job.State.Command, job.ScheduledAt);
            job.Status = CronChildJobStatus.WaitToSchedule;
        }

        private async Task CheckChildJobStatus(CronJobStateChild job)
        {
            IJobGrain grain = GrainFactory.GetGrain<IJobGrain>(job.Id);
            job.Status = await grain.GetStatus() switch
            {
                ExecutionStatus.Created => CronChildJobStatus.WaitToSchedule,
                ExecutionStatus.Scheduled or ExecutionStatus.Started => CronChildJobStatus.Scheduled,
                ExecutionStatus.Succeed => CronChildJobStatus.RanToCompletion,
                ExecutionStatus.Canceled => CronChildJobStatus.Canceled,
                ExecutionStatus.Faulted => CronChildJobStatus.Faulted,
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
            CronJobStatus.Created => Start(),
            CronJobStatus.Running => Run(),
            _ => Cleanup()
        };

        Task IRemindable.ReceiveReminder(string reminderName, TickStatus status) => Go();
        private async Task SetJobReminder(TimeSpan dueTime)
        {
            _reminder = await RegisterOrUpdateReminder("Check", dueTime, TimeSpan.FromMinutes(2));
            _logger.LogInformation($"Job Reminder Registered, dueTime={dueTime}");
        }
    }
}
