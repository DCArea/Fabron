using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;
using Fabron.Grains.TransientJob;
using Fabron.Services;

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
            ILogger<TransientJobGrain> logger,
            [PersistentState("CronJob", "JobStore")] IPersistentState<CronJobState> job,
            IMediator mediator)
        {
            _logger = logger;
            _job = job;
        }

        public Task<CronJobState> GetState()
        {
            return Task.FromResult(_job.State);
        }

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
                throw new InvalidOperationException();

            if (!_cancellationTokenSource.IsCancellationRequested)
                _cancellationTokenSource.Cancel();

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


            var firstUnfinishedJob = _job.State.UnFinishedJobs.FirstOrDefault();
            if (firstUnfinishedJob is null)
            {
                _job.State.Complete();
                _logger.LogInformation($"CronJob Finished: {_job.State.Status}");
                await Cleanup();
            }
            else
            {
                var now = DateTime.UtcNow;
                var after5Min = DateTime.UtcNow.AddMinutes(5);
                var nextSchedule = firstUnfinishedJob.ScheduledAt;
                if (firstUnfinishedJob.ScheduledAt < after5Min)
                    await SetJobReminder(TimeSpan.FromMinutes(5));
                else
                    await SetJobReminder(nextSchedule - now);
            }
        }

        private async Task ScheduleChildJobs()
        {
            DateTime utcNow = DateTime.UtcNow;
            DateTime toTime = utcNow.AddMinutes(20);
            _job.State.Schedule(toTime);
            var jobsToBeScheduled = _job.State.NotCreatedJobs.ToList();

            if (jobsToBeScheduled.Count == 0)
                return;

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
            var utcNow = DateTime.UtcNow;
            var jobsToBeChecked = _job.State.PendingJobs.Where(job => job.ScheduledAt < utcNow).ToList();
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
            ITransientJobGrain grain = GrainFactory.GetGrain<ITransientJobGrain>(job.Id);
            await grain.Create(_job.State.Command, job.ScheduledAt);
            job.Status = JobStatus.Created;
        }

        private async Task CheckChildJobStatus(CronJobStateChild job)
        {
            ITransientJobGrain grain = GrainFactory.GetGrain<ITransientJobGrain>(job.Id);
            job.Status = await grain.GetStatus();
        }

        private async Task Cleanup()
        {
            _logger.LogInformation($"Cleanup Job");
            if (_reminder is null)
                _reminder = await GetReminder("Check");
            if (_reminder is not null)
            {
                await UnregisterReminder(_reminder);
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

        Task IRemindable.ReceiveReminder(string reminderName, TickStatus status) => Go();
        private async Task SetJobReminder(TimeSpan dueTime)
        {
            _reminder = await RegisterOrUpdateReminder("Check", dueTime, TimeSpan.FromMinutes(2));
            _logger.LogInformation($"Job Reminder Registered, dueTime={dueTime}");
        }
    }
}
