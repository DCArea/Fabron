
using System;
using System.Collections.Generic;
using System.Linq;
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
            string key,
            string cronExp,
            TCommand command,
            DateTime? notBefore,
            DateTime? expirationTime,
            bool suspend,
            Dictionary<string, string>? labels,
            Dictionary<string, string>? annotations) where TCommand : ICommand
        {
            string commandName = _registry.CommandNameRegistrations[typeof(TCommand)];
            string commandData = JsonSerializer.Serialize(command);

            ICronJobGrain grain = _client.GetGrain<ICronJobGrain>(key);
            await grain.Schedule(
                cronExp,
                commandName,
                commandData,
                notBefore,
                expirationTime,
                suspend,
                labels,
                annotations);
            CronJob? state = await grain.GetState();
            return state!.Map<TCommand>();
        }

        public Task TriggerCronJob(string key)
        {
            ICronJobGrain grain = _client.GetGrain<ICronJobGrain>(key);
            return grain.Trigger();
        }

        public Task SuspendCronJob(string key)
        {
            ICronJobGrain grain = _client.GetGrain<ICronJobGrain>(key);
            return grain.Suspend();
        }

        public Task ResumeCronJob(string key)
        {
            ICronJobGrain grain = _client.GetGrain<ICronJobGrain>(key);
            return grain.Resume();
        }

        public async Task<CronJob<TCommand>?> GetCronJob<TCommand>(string key)
            where TCommand : ICommand
        {
            ICronJobGrain grain = _client.GetGrain<ICronJobGrain>(key);
            CronJob? jobState = await grain.GetState();
            if (jobState is null || jobState.Status.Deleted)
            {
                return null;
            }
            return jobState.Map<TCommand>();
        }

        public Task DeleteCronJob(string key)
            => _client.GetGrain<ICronJobGrain>(key).Delete();

        public async Task<IEnumerable<CronJob<TJobCommand>>> GetCronJobByLabel<TJobCommand>(string labelName, string labelValue)
            where TJobCommand : ICommand
        {
            IEnumerable<CronJob> jobs = await _querier.GetCronJobByLabel(labelName, labelValue);
            return jobs.Select(job => job.Map<TJobCommand>());
        }

        public async Task<IEnumerable<CronJob<TJobCommand>>> GetCronJobByLabels<TJobCommand>(params (string, string)[] labels)
            where TJobCommand : ICommand
        {
            IEnumerable<CronJob> jobs = await _querier.GetCronJobByLabels(labels);
            return jobs.Select(job => job.Map<TJobCommand>());
        }

    }
}
