// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabron.Models;
using Microsoft.Extensions.Logging;

using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Fabron.Grains
{
    public interface ICronJobGrain : IGrainWithStringKey
    {
        [ReadOnly]
        Task<CronJob> GetState();
        Task Schedule(string cronExp, string commandName, string commandData, DateTime? start, DateTime? end, Dictionary<string, string>? labels);
    }

    public class CronJobGrain : Grain, ICronJobGrain, IRemindable
    {
        private readonly ILogger _logger;
        private readonly IPersistentState<CronJob> _job;
        private readonly IJobEventBus _bus;
        private IGrainReminder? _reminder;
        private IDisposable? _timer;
        private IDisposable? _probeTimer;

        public CronJobGrain(
            ILogger<CronJobGrain> logger,
            [PersistentState("CronJob", "JobStore")] IPersistentState<CronJob> job,
            IJobEventBus bus)
        {
            _logger = logger;
            _job = job;
            _bus = bus;
        }

        private CronJob Job => _job.State;

        public Task<CronJob> GetState() => Task.FromResult(_job.State);

        public async Task Schedule(string cronExp, string commandName, string commandData, DateTime? notBefore, DateTime? expirationTime, Dictionary<string, string>? labels)
        {
            if (!_job.RecordExists)
            {
                DateTime now = DateTime.UtcNow;
                _job.State = new CronJob
                {
                    Metadata = new CronJobMetadata(this.GetPrimaryKeyString(), now, labels ?? new()),
                    Spec = new CronJobSpec(cronExp,
                        commandName,
                        commandData,
                        notBefore,
                        expirationTime),
                    Status = new CronJobStatus(new List<JobItem>())
                };
                await SaveJobStateAsync();
                _logger.LogDebug($"CronJob[{Job.Metadata.Uid}]: Created");
            }

            _reminder = await RegisterOrUpdateReminder("Check", TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(2));
            _logger.LogDebug($"CronJob[{Job.Metadata.Uid}]: reminder(Check) registered");
            await Schedule();
        }

        private async Task Schedule()
        {
            bool hasRunningJobs = Job.HasRunningJobs;
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
                DateTime? nextSchedule = Job.GetNextSchedule();
                if (nextSchedule is not null)
                {
                    DateTime now = DateTime.UtcNow;
                    _logger.LogDebug($"CronJob[{Job.Metadata.Uid}]: next schedule({nextSchedule}, now({now})");
                    TimeSpan dueTime = nextSchedule.Value >= now.AddSeconds(5) ? nextSchedule.Value.Subtract(now) : TimeSpan.Zero;
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
            List<JobItem>? jobItems = (await Task.WhenAll(checkJobStatusTasks)).ToList();
            Job.Status = Job.Status with
            {
                Jobs = jobItems.TakeLast(10).ToList()
            };
            await SaveJobStateAsync();
        }

        private async Task<JobItem> ScheduleChildJob(uint index)
        {
            string? jobId = GetChildJobIdByIndex(index);
            IJobGrain grain = GrainFactory.GetGrain<IJobGrain>(jobId);
            Dictionary<string, string>? labels = new Dictionary<string, string>
            {
                {"owner_id", Job.Metadata.Uid },
                {"owner_type" ,"cronjob"},
                {"cron_index", index.ToString() }
            };
            Job? jobState = await grain.Schedule(Job.Spec.CommandName, Job.Spec.CommandData, null, labels);
            return new JobItem(index, jobId, DateTime.UtcNow, jobState.Status.ExecutionStatus);
        }

        private async Task<JobItem> CheckJobStatus(JobItem job)
        {
            if (job.Status is ExecutionStatus.Succeed or ExecutionStatus.Faulted)
            {
                return job;
            }
            string? jobId = GetChildJobIdByIndex(job.Index);
            IJobGrain? grain = GrainFactory.GetGrain<IJobGrain>(jobId);
            ExecutionStatus status = await grain.GetStatus();
            return job with
            {
                Status = status
            };
        }

        private string GetChildJobIdByIndex(uint index) => $"cron/{Job.Metadata.Uid}/{index}";

        private async Task Complete()
        {
            if (_reminder is null)
            {
                _reminder = await GetReminder("Check");
            }
            if (_reminder is not null)
            {
                await UnregisterReminder(_reminder);
            }

            Job.Status = Job.Status with
            {
                CompletionTimestamp = DateTime.UtcNow,
            };
            await _bus.OnCronJobFinalized(Job);
            await SaveJobStateAsync();
        }

        private async Task ScheduleNextJob()
        {
            JobItem? latestJob = Job.LatestItem;
            uint latestIndex = latestJob is null ? 0 : latestJob.Index;
            JobItem? jobItem = await ScheduleChildJob(latestIndex + 1);
            List<JobItem>? items = Job.Status.Jobs;
            items.Add(jobItem);
            Job.Status = Job.Status with
            {
                Jobs = items.TakeLast(10).ToList()
            };
            await SaveJobStateAsync();
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
            {
                _probeTimer = RegisterTimer(_ => UpdateJobStatus(), null, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(20));
            }
        }
        private void StopProbeTimer() => _probeTimer?.Dispose();

        Task IRemindable.ReceiveReminder(string reminderName, TickStatus status) => Schedule();


        private async Task SaveJobStateAsync()
        {
            Job.Version += 1;
            await _job.WriteStateAsync();

            if (!Job.Status.Finalized)
            {
                await _bus.OnCronJobStateChanged(Job);
            }
        }
    }
}
