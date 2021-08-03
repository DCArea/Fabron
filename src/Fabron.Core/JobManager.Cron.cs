// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Fabron.Contracts;
using Fabron.Grains.CronJob;
using Fabron.Grains.TransientJob;
using Fabron.Mando;

using Microsoft.Extensions.Logging;

namespace Fabron
{
    public partial class JobManager
    {
        public async Task<CronJob> Schedule<TCommand>(string jobId, string cronExp, TCommand command)
            where TCommand : ICommand
        {
            string commandName = _registry.CommandNameRegistrations[typeof(TCommand)];
            string commandData = JsonSerializer.Serialize(command);

            CronJobState state = await Schedule(jobId, cronExp, new(commandName, commandData));
            return state.Map<TCommand>();
        }

        private async Task<CronJobState> Schedule(string jobId, string cronExp, Grains.JobCommandInfo command)
        {
            _logger.LogInformation($"Creating Job[{jobId}]");
            ICronJobGrain grain = _client.GetGrain<ICronJobGrain>(jobId);
            await grain.Create(cronExp, command);
            _logger.LogInformation($"Job[{jobId}] Created");

            return new CronJobState(cronExp, command);
        }


        public async Task<CronJob?> GetCronJob(string jobId)
        {
            ICronJobGrain grain = _client.GetGrain<ICronJobGrain>(jobId);
            CronJobState? jobState = await grain.GetState();
            if (jobState is null)
            {
                return null;
            }

            return jobState.Map(_registry);
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
            string cmdName = jobState.Command.Name;
            ICommand cmdData = (ICommand)JsonSerializer.Deserialize(jobState.Command.Data, _registry.CommandTypeRegistrations[cmdName])!;

            IEnumerable<Task<CronChildJobDetail>> getCreatedJobTasks = jobState.ScheduledJobs.Select(job => GetCreatedChildJobDetail(job));
            CronChildJobDetail[] createdJobs = await Task.WhenAll(getCreatedJobTasks);

            IEnumerable<CronChildJobDetail> pendingJobs = createdJobs.Where(job => job.IsPending());
            IEnumerable<CronChildJobDetail> finishedJobs = createdJobs.Where(job => job.IsFinished());


            return new CronJobDetail(
                jobState.CronExp,
                cmdData,
                pendingJobs,
                finishedJobs,
                (JobStatus)(int)jobState.Status,
                jobState.Reason);
        }

        private async Task<CronChildJobDetail> GetCreatedChildJobDetail(CronJobStateChild childState)
        {
            JobState? jobState = await GetTransientJobState(childState.Id);
            if (jobState is null)
            {
                throw new Exception();
            }

            object? result = jobState.Command.Result is null ? null : JsonSerializer.Deserialize(jobState.Command.Result, _registry.ResultTypeRegistrations[jobState.Command.Name])!;

            return new CronChildJobDetail(
                childState.Id,
                result,
                (JobStatus)(int)jobState.Status,
                jobState.CreatedAt,
                jobState.ScheduledAt,
                jobState.StartedAt,
                jobState.FinishedAt);
        }
    }
}
