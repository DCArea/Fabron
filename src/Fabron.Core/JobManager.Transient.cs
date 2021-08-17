// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

using Fabron.Contracts;
using Fabron.Grains.Job;
using Fabron.Mando;

using Microsoft.Extensions.Logging;

namespace Fabron
{
    public partial class JobManager
    {
        public async Task<Job<TCommand, TResult>> Schedule<TCommand, TResult>(string jobId, TCommand command, DateTime? scheduledAt = null, Dictionary<string,string>? labels = null)
            where TCommand : ICommand<TResult>
        {
            string commandName = _registry.CommandNameRegistrations[typeof(TCommand)];
            string commandData = JsonSerializer.Serialize(command);

            JobState state = await Schedule(jobId, commandName, commandData, scheduledAt, labels);
            return state.Map<TCommand, TResult>();
        }

        private async Task<JobState> Schedule(string jobId, string commandName, string commandData, DateTime? scheduledAt, Dictionary<string,string>? labels = null)
        {
            _logger.LogInformation($"Creating Job[{jobId}]");
            IJobGrain grain = _client.GetGrain<IJobGrain>(jobId);
            await grain.Schedule(commandName, commandData, scheduledAt, labels);
            _logger.LogInformation($"Job[{jobId}] Created");

            JobState? state = await grain.GetState();
            return state!;
        }

        public async Task<Job<TJobCommand, TResult>?> GetJobById<TJobCommand, TResult>(string jobId)
            where TJobCommand : ICommand<TResult>
        {
            JobState? jobState = await GetTransientJobState(jobId);
            if (jobState is null)
            {
                return null;
            }

            return jobState.Map<TJobCommand, TResult>();
        }

        private async Task<JobState?> GetTransientJobState(string jobId)
        {
            IJobGrain grain = _client.GetGrain<IJobGrain>(jobId);
            return await grain.GetState();
        }
    }
}
