// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Fabron.Contracts;
using Fabron.Grains.BatchJob;
using Fabron.Mando;

using Microsoft.Extensions.Logging;

namespace Fabron
{
    public partial class JobManager
    {
        public async Task Schedule(string jobId, IEnumerable<ICommand> commands)
        {
            List<Grains.JobCommandInfo>? cmds = commands.Select(cmd =>
            {
                Type cmdType = cmd.GetType();
                string cmdName = _registry.CommandNameRegistrations[cmdType];
                string cmdData = JsonSerializer.Serialize(cmd, cmdType);
                return new Grains.JobCommandInfo(cmdName, cmdData);
            }).ToList();

            _logger.LogInformation($"Creating Job[{jobId}]");
            IBatchJobGrain grain = _client.GetGrain<IBatchJobGrain>(jobId);
            await grain.Create(cmds);
            _logger.LogInformation($"Job[{jobId}] Created");

            //var state = new BatchJobState(cmds);
            //return state.Map(_registry);
        }

        public async Task<BatchJob?> GetBatchJobById(string jobId)
        {
            IBatchJobGrain grain = _client.GetGrain<IBatchJobGrain>(jobId);
            BatchJobState? jobState = await grain.GetState();
            if (jobState is null)
            {
                return null;
            }

            return jobState.Map(_registry);
        }
    }
}
