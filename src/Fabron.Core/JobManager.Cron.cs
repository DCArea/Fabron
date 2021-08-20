// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Fabron.Contracts;
using Fabron.Grains.CronJob;
using Fabron.Grains.Job;
using Fabron.Mando;

using Microsoft.Extensions.Logging;

namespace Fabron
{
    public partial class JobManager
    {
        public async Task<CronJob<TCommand>> Schedule<TCommand>(string jobId, string cronExp, TCommand command, Dictionary<string, string>? labels)
            where TCommand : ICommand
        {
            string commandName = _registry.CommandNameRegistrations[typeof(TCommand)];
            string commandData = JsonSerializer.Serialize(command);

            CronJobState state = await Schedule(jobId, cronExp, commandName, commandData, labels);
            return state.Map<TCommand>();
        }

        private async Task<CronJobState> Schedule(string jobId, string cronExp, string commandName, string commandData, Dictionary<string, string>? labels)
        {
            _logger.LogInformation($"Creating CronJob[{jobId}]");
            ICronJobGrain grain = _client.GetGrain<ICronJobGrain>(jobId);
            await grain.Schedule(cronExp, commandName, commandData, labels);
            _logger.LogInformation($"CronJob[{jobId}] Created");

            return await grain.GetState();
        }


        public async Task<CronJob<TCommand>?> GetCronJob<TCommand>(string jobId)
            where TCommand : ICommand
        {
            ICronJobGrain grain = _client.GetGrain<ICronJobGrain>(jobId);
            CronJobState? jobState = await grain.GetState();
            if (jobState is null)
            {
                return null;
            }

            return jobState.Map<TCommand>();
        }

        public async Task<CronJobDetail?> GetCronJobDetail(string jobId)
        {
            ICronJobGrain grain = _client.GetGrain<ICronJobGrain>(jobId);
            CronJobState? jobState = await grain.GetState();
            if (jobState is null)
            {
                return null;
            }

            return await GetCronJobDetail(jobState);
        }

        private async Task<CronJobDetail> GetCronJobDetail(CronJobState jobState)
        {
            string cmdName = jobState.Spec.CommandName;
            ICommand cmdData = (ICommand)JsonSerializer.Deserialize(jobState.Spec.CommandData, _registry.CommandTypeRegistrations[cmdName])!;

            IEnumerable<Task<CronChildJobDetail>> getCreatedJobTasks = jobState.Status.Jobs.Select(job => GetCreatedChildJobDetail(job));
            CronChildJobDetail[] createdJobs = await Task.WhenAll(getCreatedJobTasks);

            return new CronJobDetail(
                jobState.Spec.Schedule,
                cmdData,
                createdJobs,
                jobState.Status.Reason);
        }

        private async Task<CronChildJobDetail> GetCreatedChildJobDetail(JobItem childState)
        {
            JobState? jobState = await GetTransientJobState(childState.Uid);
            if (jobState is null)
            {
                throw new Exception();
            }

            object? result = jobState.Status.Result is null ? null : JsonSerializer.Deserialize(jobState.Status.Result, _registry.ResultTypeRegistrations[jobState.Spec.CommandName])!;

            return new CronChildJobDetail(
                childState.Uid,
                result,
                jobState.Status.ExecutionStatus,
                jobState.Metadata.CreationTimestamp,
                jobState.Spec.Schedule,
                jobState.Status.StartedAt,
                jobState.Status.FinishedAt);
        }
    }
}
