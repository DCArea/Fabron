
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

using Fabron.Contracts;
using Fabron.Grains;
using Fabron.Mando;
using Fabron.Models;

namespace Fabron
{
    public partial class JobManager
    {
        public async Task<CronJob<TCommand>> ScheduleCronJob<TCommand>(
            string name,
            string @namespace,
            TCommand command,
            string cronExp,
            DateTimeOffset? notBefore = null,
            DateTimeOffset? expirationTime = null,
            bool suspend = false,
            Dictionary<string, string>? labels = null,
            Dictionary<string, string>? annotations = null) where TCommand : ICommand
        {
            string key = KeyUtils.BuildCronJobKey(name, @namespace);

            var cmd = new CommandSpec
            {
                Name = _registry.CommandNameRegistrations[typeof(TCommand)],
                Data = JsonSerializer.Serialize(command)
            };

            ICronJobGrain grain = _client.GetGrain<ICronJobGrain>(key);
            await grain.Schedule(
                cronExp,
                cmd,
                notBefore,
                expirationTime,
                suspend,
                labels,
                annotations);
            CronJob? state = await grain.GetState();
            return state!.Map<TCommand>();
        }

        public async Task<CronJob<TCommand>?> GetCronJob<TCommand>(string name, string @namespace) where TCommand : ICommand
        {
            string key = KeyUtils.BuildCronJobKey(name, @namespace);

            ICronJobGrain grain = _client.GetGrain<ICronJobGrain>(key);
            CronJob? jobState = await grain.GetState();
            if (jobState is null || jobState.Deleted)
            {
                return null;
            }
            return jobState.Map<TCommand>();
        }

    }
}
