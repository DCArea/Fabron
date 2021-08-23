﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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
        public async Task<CronJob<TCommand>> Schedule<TCommand>(string jobId, string cronExp, TCommand command, Dictionary<string, string>? labels)
            where TCommand : ICommand
        {
            string commandName = _registry.CommandNameRegistrations[typeof(TCommand)];
            string commandData = JsonSerializer.Serialize(command);

            CronJob state = await Schedule(jobId, cronExp, commandName, commandData, labels);
            return state.Map<TCommand>();
        }

        private async Task<CronJob> Schedule(string jobId, string cronExp, string commandName, string commandData, Dictionary<string, string>? labels)
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
            CronJob? jobState = await grain.GetState();
            if (jobState is null)
            {
                return null;
            }

            return jobState.Map<TCommand>();
        }
    }
}
