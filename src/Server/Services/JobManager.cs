using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using TGH.Server.Entities;
using TGH.Server.Grains;

namespace TGH.Server.Services
{
    public interface IJobManager
    {
        Task<Job<TJobCommand, TResult>> Enqueue<TJobCommand, TResult>(Guid jobId, TJobCommand command) where TJobCommand : ICommand<TResult>;
        Task<Job<TJobCommand, TResult>> GetJobById<TJobCommand, TResult>(Guid jobId) where TJobCommand : ICommand<TResult>;
    }

    public class JobManager : IJobManager
    {
        private readonly ILogger _logger;
        private readonly JobOptions _options;
        private readonly IClusterClient _client;
        public JobManager(ILogger<JobManager> logger,
            IOptions<JobOptions> options,
            IClusterClient client)
        {
            _logger = logger;
            _options = options.Value;
            _client = client;
        }

        public async Task<Job<TJobCommand, TResult>> Enqueue<TJobCommand, TResult>(Guid jobId, TJobCommand command)
            where TJobCommand : ICommand<TResult>
        {
            var commandName = _options.CommandRegistrations[typeof(TJobCommand)];
            var commandData = JsonSerializer.Serialize(command);

            await Enqueue(jobId, commandName, commandData);

            var job = new Job<TJobCommand, TResult>(
                commandName,
                command,
                default(TResult),
                JobStatus.Created,
                null
            );
            return job;
        }

        private async Task Enqueue(Guid jobId, string commandName, string commandData)
        {
            _logger.LogInformation($"Creating Job[{jobId}]");
            var grain = _client.GetGrain<IJobGrain>(jobId);
            await grain.Create(commandName, commandData);
            _logger.LogInformation($"Job[{jobId}] Created");
        }

        public async Task<Job<TJobCommand, TResult>> GetJobById<TJobCommand, TResult>(Guid jobId)
            where TJobCommand : ICommand<TResult>
        {
            var grain = _client.GetGrain<IJobGrain>(jobId);
            var jobState = await grain.GetState();

            var job = new Job<TJobCommand, TResult>(
                jobState.Command.Name,
                JsonSerializer.Deserialize<TJobCommand>(jobState.Command.Data)!,
                jobState.Command.Result is null
                    ? default
                    : JsonSerializer.Deserialize<TResult>(jobState.Command.Result),
                JobStatus.Created,
                null
            );
            return job;
        }
    }
}
