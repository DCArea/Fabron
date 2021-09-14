
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Fabron.Contracts;
using Fabron.Grains;
using Fabron.Mando;
using Fabron.Models;
using Microsoft.Extensions.Logging;

namespace Fabron
{
    public partial class JobManager
    {
        public async Task<Job<TCommand, TResult>> ScheduleJob<TCommand, TResult>(
            string jobId,
            TCommand command,
            DateTime? scheduledAt,
            Dictionary<string, string>? labels,
            Dictionary<string, string>? annotations)
            where TCommand : ICommand<TResult>
        {
            string commandName = _registry.CommandNameRegistrations[typeof(TCommand)];
            string commandData = JsonSerializer.Serialize(command);

            Job state = await Schedule(jobId, commandName, commandData, scheduledAt, labels, annotations);
            return state.Map<TCommand, TResult>();
        }

        private async Task<Job> Schedule(
            string jobId,
            string commandName,
            string commandData,
            DateTime? scheduledAt,
            Dictionary<string, string>? labels,
            Dictionary<string, string>? annotations)
        {
            _logger.LogInformation($"Creating Job[{jobId}]");
            IJobGrain grain = _client.GetGrain<IJobGrain>(jobId);
            await grain.Schedule(commandName, commandData, scheduledAt, labels, annotations);
            _logger.LogInformation($"Job[{jobId}] Created");

            Job? state = await grain.GetState();
            return state!;
        }

        public async Task<Job<TJobCommand, TResult>?> GetJobById<TJobCommand, TResult>(string jobId)
            where TJobCommand : ICommand<TResult>
        {
            IJobGrain grain = _client.GetGrain<IJobGrain>(jobId);
            Job? jobState = await grain.GetState();
            if (jobState is null || jobState.Status.Deleted)
            {
                return null;
            }

            return jobState.Map<TJobCommand, TResult>();
        }

        public Task DeleteJobById(string jobId)
            => _client.GetGrain<IJobGrain>(jobId).Delete();

        public async Task<IEnumerable<Job<TJobCommand, TResult>>> GetJobByLabel<TJobCommand, TResult>(string labelName, string labelValue)
            where TJobCommand : ICommand<TResult>
        {
            IEnumerable<Job> jobs = await _querier.GetJobByLabel(labelName, labelValue);
            return jobs.Select(job => job.Map<TJobCommand, TResult>());
        }

        public async Task<IEnumerable<Job<TJobCommand, TResult>>> GetJobByLabels<TJobCommand, TResult>(params (string, string)[] labels)
            where TJobCommand : ICommand<TResult>
        {
            IEnumerable<Job> jobs = await _querier.GetJobByLabels(labels);
            return jobs.Select(job => job.Map<TJobCommand, TResult>());
        }

        public async Task<IEnumerable<Job<TJobCommand, TResult>>> GetJobByCron<TJobCommand, TResult>(string cronJobId)
            where TJobCommand : ICommand<TResult>
        {
            (string, string)[]? labels = new[] { ("owner_type", "cronjob"), ("owner_id", cronJobId) };
            IEnumerable<Job> jobs = await _querier.GetJobByLabels(labels);
            return jobs.Select(job => job.Map<TJobCommand, TResult>());
        }
    }
}
