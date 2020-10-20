using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;
using TGH.Server.Services;

namespace TGH.Server.Grains.BatchJob
{
    public interface IBatchJobGrain : IGrainWithGuidKey
    {
        [AlwaysInterleave]
        Task Cancel(string reason);
        [ReadOnly]
        Task<BatchJobState> GetState();
        Task Create(CreateBatchJob job);
    }

    public class BatchJobGrain : Grain, IBatchJobGrain, IRemindable
    {
        private readonly ILogger _logger;
        private readonly IPersistentState<BatchJobState> _job;
        private IGrainReminder? reminder;
        private CancellationTokenSource? cancellationTokenSource;

        public BatchJobGrain(
            ILogger<JobGrain> logger,
            [PersistentState("BatchJob", "JobStore")] IPersistentState<BatchJobState> job,
            IMediator mediator)
        {
            _logger = logger;
            _job = job;
        }

        public Task<BatchJobState> GetState()
        {
            return Task.FromResult(_job.State);
        }

        public async Task Create(CreateBatchJob job)
        {
            if (!_job.RecordExists)
            {
                _job.State = job.Create();
                await _job.WriteStateAsync();
                _logger.LogInformation($"Created Job");
            }

            reminder = await RegisterOrUpdateReminder("Check", TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(10));
            _logger.LogInformation($"Job Reminder Registered");
            _ = Go();
        }

        public async Task Cancel(string reason)
        {
            if (cancellationTokenSource is null)
                throw new InvalidOperationException();

            if (!cancellationTokenSource.IsCancellationRequested)
                cancellationTokenSource.Cancel();

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
            _logger.LogInformation($"Run Job");
            cancellationTokenSource ??= new CancellationTokenSource(TimeSpan.FromMinutes(30));

            while (true)
            {
                var pendingJobs = _job.State.PendingJobs.Take(10).ToList();
                if (pendingJobs.Count == 0)
                {
                    break;
                }

                var createChildJobTasks = pendingJobs
                    .Where(job => job.Status == JobStatus.NotCreated)
                    .Select(job => CreateChildJob(job));
                var checkJobStatusTasks = pendingJobs
                    .Select(job => CheckChildJobStatus(job));

                await Task.WhenAll(createChildJobTasks);
                await Task.WhenAll(checkJobStatusTasks);
                await _job.WriteStateAsync();
            }


            _logger.LogInformation($"Job Finished: {_job.State.Status}");
            await Cleanup();

            async Task CreateChildJob(ChildJobState job)
            {
                var grain = GrainFactory.GetGrain<IJobGrain>(job.Id);
                await grain.Create(job.Command.Name, job.Command.Data);
            }
            async Task CheckChildJobStatus(ChildJobState job)
            {
                var grain = GrainFactory.GetGrain<IJobGrain>(job.Id);
                job.Status = await grain.GetStatus();
            }
        }

        private async Task Cleanup()
        {
            _logger.LogInformation($"Cleanup Job");
            if (reminder is null)
                reminder = await GetReminder("Check");
            if (reminder is not null)
            {
                await UnregisterReminder(reminder);
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
    }
}
