// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TGH.Contracts;
using TGH.Grains.BatchJob;

namespace TGH
{
    public partial class JobManager
    {
        public async Task Schedule(Guid jobId, IEnumerable<ICommand> commands)
        {
            var cmds = commands.Select(cmd =>
            {
                Type cmdType = cmd.GetType();
                string cmdName = _registry.CommandNameRegistrations[cmdType];
                string cmdData = JsonSerializer.Serialize(cmd, cmdType);
                return new Grains.JobCommandInfo(cmdName, cmdData);
            }).ToList();

            _logger.LogInformation($"Creating Job[{jobId}]");
            var grain = _client.GetGrain<IBatchJobGrain>(jobId);
            await grain.Create(cmds);
            _logger.LogInformation($"Job[{jobId}] Created");

            //var state = new BatchJobState(cmds);
            //return state.Map(_registry);
        }

        public async Task<BatchJob?> GetBatchJobById(Guid jobId)
        {
            var grain = _client.GetGrain<IBatchJobGrain>(jobId);
            var jobState = await grain.GetState();
            if (jobState is null)
                return null;
            return jobState.Map(_registry);
        }
    }
}
