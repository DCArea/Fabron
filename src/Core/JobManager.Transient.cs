// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Fabron.Contracts;
using Fabron.Grains.TransientJob;

namespace Fabron
{
    public partial class JobManager
    {
        public async Task<TransientJob<TCommand, TResult>> Schedule<TCommand, TResult>(string jobId, TCommand command, DateTime? scheduledAt = null)
            where TCommand : ICommand<TResult>
        {
            string commandName = _registry.CommandNameRegistrations[typeof(TCommand)];
            string commandData = JsonSerializer.Serialize(command);

            var state = await Schedule(jobId, new(commandName, commandData), scheduledAt);
            return state.Map<TCommand, TResult>();
        }

        private async Task<TransientJobState> Schedule(string jobId, Grains.JobCommandInfo command, DateTime? scheduledAt)
        {
            _logger.LogInformation($"Creating Job[{jobId}]");
            var grain = _client.GetGrain<ITransientJobGrain>(jobId);
            await grain.Create(command, scheduledAt);
            _logger.LogInformation($"Job[{jobId}] Created");

            return new TransientJobState(command, scheduledAt);
        }

        public async Task<TransientJob<TJobCommand, TResult>?> GetJobById<TJobCommand, TResult>(string jobId)
            where TJobCommand : ICommand<TResult>
        {
            TransientJobState? jobState = await GetTransientJobState(jobId);
            if (jobState is null)
                return null;
            return jobState.Map<TJobCommand, TResult>();
        }

        private async Task<TransientJobState?> GetTransientJobState(string jobId)
        {
            var grain = _client.GetGrain<ITransientJobGrain>(jobId);
            return await grain.GetState();
        }
    }
}
