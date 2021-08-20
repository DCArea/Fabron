// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
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
        //[AlwaysInterleave]
        //Task Cancel(string reason);
        [ReadOnly]
        Task<CronJobState> GetState();
        Task Schedule(string cronExp, string commandName, string commandData, Dictionary<string, string>? labels = null);
    }

    public class CronJobGrain : Grain, ICronJobGrain, IRemindable
    {
        private readonly ILogger _logger;
        private readonly IPersistentState<CronJobState> _job;
        private IGrainReminder? _reminder;
        private IDisposable? _timer;
        private IDisposable? _probeTimer;

        public CronJobGrain(
            ILogger<CronJobGrain> logger,
            [PersistentState("CronJob", "JobStore")] IPersistentState<CronJobState> job,
            IMediator mediator)
        {
            _logger = logger;
            _job = job;
        }

        private CronJobState Job => _job.State;

        public Task<CronJobState> GetState() => Task.FromResult(_job.State);

        public async Task Schedule(string cronExp, string commandName, string commandData, Dictionary<string, string>? labels = null)
        {
            if (!_job.RecordExists)
            {
                var now = DateTime.UtcNow;
                _job.State = new CronJobState
                {
                    Metadata = new CronJobMetadata(this.GetPrimaryKeyString(), now, labels ?? new()),
                    Spec = new CronJobSpec(cronExp,
                        commandName,
                        commandData,
                        now,
                        DateTime.MaxValue),
                    Status = new CronJobStatus(new List<JobItem>())
                };
                await _job.WriteStateAsync();
                _logger.LogDebug($"CronJob[{Job.Metadata.Uid}]: Created");
            }

            _reminder = await RegisterOrUpdateReminder("Check", TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(2));
            _logger.LogDebug($"CronJob[{Job.Metadata.Uid}]: reminder(Check) registered");
            await Schedule();
        }

        private async Task Schedule()
        {
            var hasRunningJobs = Job.HasRunningJobs;
            if (hasRunningJobs)
            {
                EnsureProbeTimer();
                _logger.LogDebug($"CronJob[{Job.Metadata.Uid}]: There're running jobs, ensure probe timer registered");
            }
            else
            {
                StopProbeTimer();
            }
            while (true)
            {
                var nextSchedule = Job.GetNextSchedule();
                if (nextSchedule is not null)
                {
                    var now = DateTime.UtcNow;
                    _logger.LogDebug($"CronJob[{Job.Metadata.Uid}]: next schedule({nextSchedule}, now({now})");
                    var dueTime = nextSchedule.Value >= now.AddSeconds(5) ? nextSchedule.Value.Subtract(now) : TimeSpan.Zero;
                    if (dueTime > TimeSpan.Zero)
                    {
                        await ScheduleAfter(dueTime);
                        return;
                    }
                    else
                    {
                        await ScheduleNextJob();
                    }
                }
                else if (!hasRunningJobs)
                {
                    await Complete();
                    _logger.LogDebug($"CronJob[{Job.Metadata.Uid}]: Completed");
                    return;
                }
            }
        }

        private async Task UpdateJobStatus()
        {
            IEnumerable<Task<JobItem>> checkJobStatusTasks = Job.Status.Jobs
                .Select(job => CheckJobStatus(job));
            var jobItems = (await Task.WhenAll(checkJobStatusTasks)).ToList();
            Job.Status = Job.Status with
            {
                Jobs = jobItems.TakeLast(10).ToList()
            };
            await _job.WriteStateAsync();
        }

        private async Task<JobItem> ScheduleChildJob(uint index)
        {
            var jobId = GetChildJobIdByIndex(index);
            IJobGrain grain = GrainFactory.GetGrain<IJobGrain>(jobId);
            var labels = new Dictionary<string, string>
            {
                {"owner_id", Job.Metadata.Uid },
                {"owner_type" ,"cronjob"},
                {"cron_index", index.ToString() }
            };
            var jobState = await grain.Schedule(Job.Spec.CommandName, Job.Spec.CommandData, null, labels);
            return new JobItem(index, jobId, DateTime.UtcNow, jobState.Status.ExecutionStatus);
        }

        private async Task<JobItem> CheckJobStatus(JobItem job)
        {
            if (job.Status is ExecutionStatus.Succeed or ExecutionStatus.Faulted)
            {
                return job;
            }
            var jobId = GetChildJobIdByIndex(job.Index);
            var grain = GrainFactory.GetGrain<IJobGrain>(jobId);
            var status = await grain.GetStatus();
            return job with
            {
                Status = status
            };
        }

        private string GetChildJobIdByIndex(uint index) => $"cron/{Job.Metadata.Uid}/{index}";

        private async Task Complete()
        {
            Job.Status = Job.Status with
            {
                CompletionTimestamp = DateTime.UtcNow,
            };
            await _job.WriteStateAsync();
            if (_reminder is null)
            {
                _reminder = await GetReminder("Check");
            }
            if (_reminder is not null)
            {
                await UnregisterReminder(_reminder);
            }
            return;
        }

        private async Task ScheduleNextJob()
        {
            var latestJob = Job.LatestItem;
            var latestIndex = latestJob is null ? 0 : latestJob.Index;
            var jobItem = await ScheduleChildJob(latestIndex + 1);
            var items = Job.Status.Jobs;
            items.Add(jobItem);
            Job.Status = Job.Status with
            {
                Jobs = items.TakeLast(10).ToList()
            };
            await _job.WriteStateAsync();
            EnsureProbeTimer();
            _logger.LogDebug($"CronJob[{Job.Metadata.Uid}]: Scheduled job-{jobItem.Index}");
        }


        private async Task ScheduleAfter(TimeSpan dueTime)
        {
            _timer?.Dispose();
            if (dueTime.TotalMinutes < 2)
            {
                _timer = RegisterTimer(_ => Schedule(), null, dueTime, TimeSpan.FromMilliseconds(-1));
            }
            else
            {
                _reminder = await RegisterOrUpdateReminder("Check", dueTime, TimeSpan.FromMinutes(2));
            }
            _logger.LogDebug($"CronJob[{Job.Metadata.Uid}]: Scheduled After {dueTime}");
        }

        private void EnsureProbeTimer()
        {
            if (_probeTimer is null)
                _probeTimer = RegisterTimer(_ => UpdateJobStatus(), null, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(20));
        }
        private void StopProbeTimer()
        {
            _probeTimer?.Dispose();
        }

        Task IRemindable.ReceiveReminder(string reminderName, TickStatus status) => Schedule();
    }
}
