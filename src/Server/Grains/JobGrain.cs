using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;
using TGH.Server.Entities;

namespace TGH.Server.Grains
{
    public class JobState<TCommand, TResult> where TCommand : ICommand<TResult>
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public JobState() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public JobState(TCommand command)
        {
            Command = command;
            Status = JobStatus.Created;
        }

        public TCommand Command { get; }
        public TResult? Result { get; private set; }
        public JobStatus Status { get; private set; }
        public string? Reason { get; private set; }

        public void Start()
        {
            Status = JobStatus.Running;
        }

        public void Complete(TResult result)
        {
            Status = JobStatus.RanToCompletion;
            Result = result;
        }

        public void Cancel(string reason)
        {
            Status = JobStatus.Canceled;
            Reason = reason;
        }

        public void Fault(Exception exception)
        {
            Status = JobStatus.Canceled;
            Reason = exception.ToString();
        }
    }

    public enum JobStatus
    {
        Created,
        Running,
        RanToCompletion,
        Canceled,
        Faulted
    }

    public interface IJobGrain<TCommand, TResult> : IGrainWithGuidKey
        where TCommand : ICommand<TResult>
    {
        [AlwaysInterleave]
        Task Cancel(string reason);
        [ReadOnly]
        Task<JobState<TCommand, TResult>> GetState();
        Task Create(TCommand command);
    }

    public class JobGrain<TCommand, TResult> : Grain, IJobGrain<TCommand, TResult>, IRemindable
        where TCommand : ICommand<TResult>
    {
        private readonly ILogger<JobGrain<TCommand, TResult>> _logger;
        private readonly IPersistentState<JobState<TCommand, TResult>> _job;
        private IGrainReminder? reminder;
        private CancellationTokenSource? cancellationTokenSource;

        public JobGrain(
          ILogger<JobGrain<TCommand, TResult>> logger,
          [PersistentState("Job", "JobStore")] IPersistentState<JobState<TCommand, TResult>> job)
        {
            _logger = logger;
            _job = job;
        }

        public Task<JobState<TCommand, TResult>> GetState()
        {
            return Task.FromResult(_job.State);
        }

        public async Task Create(TCommand command)
        {
            if (_job.RecordExists)
                return;

            _job.State = new JobState<TCommand, TResult>(command);
            await _job.WriteStateAsync();
            _logger.LogInformation($"Created Job");
            reminder = await RegisterOrUpdateReminder("Check", TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(20));
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
            cancellationTokenSource ??= new CancellationTokenSource(TimeSpan.FromMinutes(10));
            try
            {
                var handler = ServiceProvider.GetRequiredService<ICommandHandler<TCommand, TResult>>();
                var result = await handler.Handle(_job.State.Command, cancellationTokenSource.Token);
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
            JobStatus.Created => Start(),
            JobStatus.Running => Run(),
            _ => Cleanup()
        };

        Task IRemindable.ReceiveReminder(string reminderName, TickStatus status) => Go();
    }
}
