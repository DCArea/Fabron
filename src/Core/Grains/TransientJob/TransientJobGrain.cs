using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;
using TGH.Services;

namespace TGH.Grains.TransientJob
{

    public interface ITransientJobGrain : IGrainWithGuidKey
    {
        [AlwaysInterleave]
        Task Cancel(string reason);
        [ReadOnly]
        Task<TransientJobState> GetState();
        [ReadOnly]
        Task<JobStatus> GetStatus();
        Task Create(JobCommandInfo command);
    }

    public class TransientJobGrain : Grain, ITransientJobGrain, IRemindable
    {
        private readonly ILogger _logger;
        private readonly IPersistentState<TransientJobState> _job;
        private readonly IMediator _mediator;
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

        public Task<TransientJobState> GetState() => Task.FromResult(_job.State);
        public Task<JobStatus> GetStatus() => Task.FromResult(_job.State.Status);

        public async Task Create(JobCommandInfo command)
        {
            if (!_job.RecordExists)
            {
                _job.State = new TransientJobState(command.Name, command.Data);
                await _job.WriteStateAsync();
                _logger.LogInformation($"Created Job");
            }

            _reminder = await RegisterOrUpdateReminder("Check", TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(20));
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
            _logger.LogInformation($"Run Job");
            _cancellationTokenSource ??= new CancellationTokenSource(TimeSpan.FromMinutes(10));
            try
            {
                string? result = await _mediator.Handle(_job.State.Command.Name, _job.State.Command.Data, _cancellationTokenSource.Token);
                _job.State.Complete(result);
            }
            catch (Exception e)
            {
                if (e is not TaskCanceledException || _job.State.Status != JobStatus.Canceled)
                {
                    _job.State.Fault(e);
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
                _reminder = await GetReminder("Check");
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

        Task IRemindable.ReceiveReminder(string reminderName, TickStatus status) => Go();
    }
}
