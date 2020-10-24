using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;
using TGH.Grains.TransientJob;
using TGH.Services;

namespace TGH.Grains.BatchJob
{
    public interface IBatchJobGrain : IGrainWithGuidKey
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
            ILogger<TransientJobGrain> logger,
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
                var pendingJobs = _job.State.PendingJobs.Take(10).ToList();
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
                var jobsToCheck = _job.State.EnqueuedJobs.ToList();
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
            ITransientJobGrain grain = GrainFactory.GetGrain<ITransientJobGrain>(job.Id);
            await grain.Create(job.Command);
            job.Status = JobStatus.Created;
        }

        private async Task CheckChildJobStatus(BatchJobStateChild job)
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
    }
}
