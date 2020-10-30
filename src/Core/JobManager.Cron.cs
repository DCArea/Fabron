// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Fabron.Contracts;
using Fabron.Grains.CronJob;
using Fabron.Grains.TransientJob;
using Fabron.Mando;

namespace Fabron
{
    public partial class JobManager
    {
        public async Task<CronJob> Schedule<TCommand>(string jobId, string cronExp, TCommand command)
            where TCommand : ICommand
        {
            string commandName = _registry.CommandNameRegistrations[typeof(TCommand)];
            string commandData = JsonSerializer.Serialize(command);

            var state = await Schedule(jobId, cronExp, new(commandName, commandData));
            return state.Map<TCommand>();
        }

        private async Task<CronJobState> Schedule(string jobId, string cronExp, Grains.JobCommandInfo command)
        {
            _logger.LogInformation($"Creating Job[{jobId}]");
            var grain = _client.GetGrain<ICronJobGrain>(jobId);
            await grain.Create(cronExp, command);
            _logger.LogInformation($"Job[{jobId}] Created");

            return new CronJobState(cronExp, command);
        }


        public async Task<CronJob?> GetCronJob(string jobId)
        {
            var grain = _client.GetGrain<ICronJobGrain>(jobId);
            var jobState = await grain.GetState();
            if (jobState is null)
                return null;
            return jobState.Map(_registry);
        }

        public async Task<CronJobDetail?> GetCronJobDetail(string jobId)
        {
            var grain = _client.GetGrain<ICronJobGrain>(jobId);
            var jobState = await grain.GetState();
            if (jobState is null)
                return null;
            return await GetCronJobDetail(jobState);
        }

        private async Task<CronJobDetail> GetCronJobDetail(CronJobState jobState)
        {
            string cmdName = jobState.Command.Name;
            var cmdData = (ICommand)JsonSerializer.Deserialize(jobState.Command.Data, _registry.CommandTypeRegistrations[cmdName])!;

            var notCreatedJobs = jobState.NotCreatedJobs.Select(job => GetNotCreatedChildJobDetail(job));
            var getCreatedJobTasks = jobState.CreatedJobs.Select(job => GetCreatedChildJobDetail(job));
            var createdJobs = await Task.WhenAll(getCreatedJobTasks);

            var pendingJobs = createdJobs.Where(job => job.IsPending());
            var finishedJobs = createdJobs.Where(job => job.IsFinished());


            return new CronJobDetail(
                jobState.CronExp,
                cmdData,
                notCreatedJobs,
                pendingJobs,
                finishedJobs,
                (JobStatus)(int)jobState.Status,
                jobState.Reason);
        }

        private async Task<CronChildJobDetail> GetCreatedChildJobDetail(CronJobStateChild childState)
        {
            var jobState = await GetTransientJobState(childState.Id);
            if (jobState is null) throw new Exception();

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

        private static CronChildJobDetail GetNotCreatedChildJobDetail(CronJobStateChild childState)
        {
            return new CronChildJobDetail(childState.Id, null, JobStatus.NotCreated, null, childState.ScheduledAt, null, null);
        }
    }
}
