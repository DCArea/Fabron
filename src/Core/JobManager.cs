using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using TGH.Contracts;
using TGH.Grains.BatchJob;
using TGH.Grains.TransientJob;
using TGH.Services;

namespace TGH
{
    public interface IJobManager
    {
        Task<TransientJob<TCommand, TResult>> Schedule<TCommand, TResult>(Guid jobId, TCommand command, DateTime? scheduledAt = null)
            where TCommand : ICommand<TResult>;
        Task<TransientJob<TCommand, TResult>?> GetJobById<TCommand, TResult>(Guid jobId)
            where TCommand : ICommand<TResult>;

        Task Enqueue(Guid jobId, IEnumerable<ICommand> commands);
        Task<BatchJob?> GetBatchJobById(Guid jobId);

    }

    public class JobManager : IJobManager
    {
        private readonly ILogger _logger;
        private readonly CommandRegistry _options;
        private readonly IClusterClient _client;
        public JobManager(ILogger<JobManager> logger,
            IOptions<CommandRegistry> options,
            IClusterClient client)
        {
            _logger = logger;
            _options = options.Value;
            _client = client;
        }

        public async Task<TransientJob<TCommand, TResult>> Schedule<TCommand, TResult>(Guid jobId, TCommand command, DateTime? scheduledAt = null)
            where TCommand : ICommand<TResult>
        {
            string commandName = _options.CommandNameRegistrations[typeof(TCommand)];
            string commandData = JsonSerializer.Serialize(command);

            var state = await Schedule(jobId, new(commandName, commandData), scheduledAt);
            return state.To<TCommand, TResult>();
        }

        private async Task<TransientJobState> Schedule(Guid jobId, Grains.JobCommandInfo command, DateTime? scheduledAt)
        {
            _logger.LogInformation($"Creating Job[{jobId}]");
            var grain = _client.GetGrain<ITransientJobGrain>(jobId);
            await grain.Create(command, scheduledAt);
            _logger.LogInformation($"Job[{jobId}] Created");

            return new TransientJobState(command, scheduledAt);
        }

        public async Task Enqueue(Guid jobId, IEnumerable<ICommand> commands)
        {
            var cmds = commands.Select(cmd =>
            {
                Type cmdType = cmd.GetType();
                string cmdName = _options.CommandNameRegistrations[cmdType];
                string cmdData = JsonSerializer.Serialize(cmd, cmdType);
                return new Grains.JobCommandInfo(cmdName, cmdData);
            }).ToList();

            _logger.LogInformation($"Creating Job[{jobId}]");
            IBatchJobGrain grain = _client.GetGrain<IBatchJobGrain>(jobId);
            await grain.Create(cmds);
            _logger.LogInformation($"Job[{jobId}] Created");
        }

        public async Task<TransientJob<TJobCommand, TResult>?> GetJobById<TJobCommand, TResult>(Guid jobId)
            where TJobCommand : ICommand<TResult>
        {
            ITransientJobGrain grain = _client.GetGrain<ITransientJobGrain>(jobId);
            TransientJobState? jobState = await grain.GetState();
            if (jobState is null)
                return null;
            TransientJob<TJobCommand, TResult>? job = jobState.To<TJobCommand, TResult>();
            return job;
        }

        public async Task<BatchJob?> GetBatchJobById(Guid jobId)
        {
            IBatchJobGrain grain = _client.GetGrain<IBatchJobGrain>(jobId);
            BatchJobState? jobState = await grain.GetState();
            if (jobState is null)
                return null;
            return jobState.To(_options);
        }
    }
}
