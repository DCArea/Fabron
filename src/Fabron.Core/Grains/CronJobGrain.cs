
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
        Task<CronJob?> GetState();

        Task Schedule(string cronExp, string commandName, string commandData, DateTime? start, DateTime? end, bool suspend, Dictionary<string, string>? labels);

        [AlwaysInterleave]
        Task Delete();

        Task Suspend();

        Task Resume();
    }

    public class CronJobGrain : Grain, ICronJobGrain, IRemindable
    {
        private readonly ILogger _logger;
        private readonly IPersistentState<CronJob> _jobState;
        private readonly IJobEventBus _bus;
        private IGrainReminder? _tickReminder;
        private IDisposable? _tickTimer;
        private IDisposable? _statusProber;

        public CronJobGrain(
            ILogger<CronJobGrain> logger,
            [PersistentState("CronJob", "JobStore")] IPersistentState<CronJob> job,
            IJobEventBus bus)
        {
            _logger = logger;
            _jobState = job;
            _bus = bus;
        }

        private CronJob Job => _jobState.State;

        public Task<CronJob?> GetState() => Task.FromResult(_jobState.RecordExists ? _jobState.State : default);

        public async Task Schedule(
            string cronExp,
            string commandName,
            string commandData,
            DateTime? notBefore,
            DateTime? expirationTime,
            bool suspend,
            Dictionary<string, string>? labels)
        {
            DateTime now = DateTime.UtcNow;
            _jobState.State = new CronJob
            {
                Metadata = new CronJobMetadata(this.GetPrimaryKeyString(), now, labels ?? new()),
                Spec = new CronJobSpec(
                    cronExp,
                    commandName,
                    commandData,
                    notBefore,
                    expirationTime,
                    suspend),
                Status = new CronJobStatus(new List<JobItem>())
            };
            await SaveJobStateAsync();
            _logger.LogDebug($"CronJob[{Job.Metadata.Uid}]: Created");

            if (!Job.Spec.Suspend)
            {
                await ScheduleNextTick();
            }
            else
            {
                await StopTicker();
            }
        }

        public async Task Delete()
        {
            await _jobState.ClearStateAsync();
            await StopTicker();
            _logger.LogDebug($"CronJob[{Job.Metadata.Uid}]: Deleted");
        }

        public async Task Suspend()
        {
            Job.Spec = Job.Spec with { Suspend = true };
            await SaveJobStateAsync();
            await StopTicker();
            _logger.LogDebug($"CronJob[{Job.Metadata.Uid}]: Suspended");
        }

        public async Task Resume()
        {
            Job.Spec = Job.Spec with { Suspend = false };
            await ScheduleNextTick();
            _logger.LogDebug($"CronJob[{Job.Metadata.Uid}]: Resumed");
        }

        private async Task Tick()
        {
            DateTime now = DateTime.UtcNow;
            DateTime notBefore = now.AddSeconds(-5);
            DateTime? tick = Job.GetNextTick(notBefore);
            if (tick is null)
            {
                await TryComplete();
            }
            else
            {
                if (tick.Value <= now.AddSeconds(5))
                {
                    await ScheduleJob();
                }

                await ScheduleNextTick();
            }
        }

        private async Task CheckJobStatus()
        {
            IEnumerable<Task<JobItem>> checkJobStatusTasks = Job.Status.Jobs
                .Select(job => Check(job));
            List<JobItem>? jobItems = (await Task.WhenAll(checkJobStatusTasks)).ToList();
            Job.Status = Job.Status with
            {
                Jobs = jobItems.TakeLast(10).ToList()
            };
            await SaveJobStateAsync();
            if (!Job.HasRunningJobs)
            {
                StopProbeTimer();
            }

            async Task<JobItem> Check(JobItem job)
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
        }


        private async Task ScheduleJob()
        {
            JobItem? latestJob = Job.LatestItem;
            uint latestIndex = latestJob is null ? 0 : latestJob.Index;
            JobItem? jobItem = await Schedule(latestIndex + 1);
            List<JobItem>? items = Job.Status.Jobs;
            items.Add(jobItem);
            Job.Status = Job.Status with
            {
                Jobs = items.TakeLast(10).ToList()
            };
            await SaveJobStateAsync();
            EnsureStatusProber();
            _logger.LogDebug($"CronJob[{Job.Metadata.Uid}]: Scheduled job-{jobItem.Index}");

            async Task<JobItem> Schedule(uint index)
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
        }

        private string GetChildJobIdByIndex(uint index) => $"cron/{Job.Metadata.Uid}/{index}";

        private async Task TryComplete()
        {
            bool hasRunningJobs = Job.HasRunningJobs;
            if (hasRunningJobs)
            {
                EnsureStatusProber();
                _logger.LogDebug($"CronJob[{Job.Metadata.Uid}]: Can not complete since there're jobs still running, try later");
                await TickAfter(TimeSpan.FromSeconds(20));
            }

            Job.Status = Job.Status with
            {
                CompletionTimestamp = DateTime.UtcNow,
            };
            await SaveJobStateAsync();
            await StopTicker();
            _logger.LogDebug($"CronJob[{Job.Metadata.Uid}]: Completed");
        }


        private async Task ScheduleNextTick()
        {
            DateTime now = DateTime.UtcNow;
            DateTime notBefore = now.AddSeconds(-5);
            DateTime nextTick = Job.GetNextTick(notBefore) ?? now;
            await TickAfter(nextTick.Subtract(now));
        }

        private async Task TickAfter(TimeSpan dueTime)
        {
            _tickTimer?.Dispose();
            if (dueTime.TotalMinutes < 2)
            {
                _tickTimer = RegisterTimer(_ => Tick(), null, dueTime, TimeSpan.FromMilliseconds(-1));
                if (_tickReminder is null)
                {
                    _tickReminder = await RegisterOrUpdateReminder("Ticker", dueTime.Add(TimeSpan.FromMinutes(2)), TimeSpan.FromMinutes(2));
                }
            }
            else
            {
                _tickReminder = await RegisterOrUpdateReminder("Ticker", dueTime, TimeSpan.FromMinutes(2));
            }
            _logger.LogDebug($"CronJob[{Job.Metadata.Uid}]: Tick After {dueTime}");
        }

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

        private void EnsureStatusProber()
        {
            if (_statusProber is null)
            {
                _statusProber = RegisterTimer(_ => CheckJobStatus(), null, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(20));
            }
        }
        private void StopProbeTimer() => _statusProber?.Dispose();

        Task IRemindable.ReceiveReminder(string reminderName, TickStatus status) => Tick();


        private async Task SaveJobStateAsync()
        {
            Job.Version += 1;
            await _jobState.WriteStateAsync();
            await _bus.OnCronJobStateChanged(Job);
        }
    }
}
