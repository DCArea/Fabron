
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Fabron.Contracts;
using Fabron.Grains;
using Fabron.Mando;
using Fabron.Models;
using Microsoft.Extensions.Logging;

using static Fabron.FabronConstants;

namespace Fabron
{

    public partial class JobManager
    {
        public async Task<Job<TCommand, TResult>> ScheduleJob<TCommand, TResult>(
            string name,
            string @namespace,
            TCommand command,
            DateTimeOffset schedule,
            Dictionary<string, string>? labels = null,
            Dictionary<string, string>? annotations = null)
            where TCommand : ICommand<TResult>
        {
            string key = KeyUtils.BuildJobKey(name, @namespace);
            string commandName = _registry.CommandNameRegistrations[typeof(TCommand)];
            string commandData = JsonSerializer.Serialize(command);
            Job state = await Schedule(key, commandName, commandData, schedule, labels, annotations);
            return state.Map<TCommand, TResult>();
        }

        private async Task<Job> Schedule(
            string key,
            string commandName,
            string commandData,
            DateTimeOffset scheduledAt,
            Dictionary<string, string>? labels,
            Dictionary<string, string>? annotations)
        {
            IJobGrain grain = _client.GetGrain<IJobGrain>(key);
            var cmd = new CommandSpec
            {
                Name = commandName,
                Data = commandData
            };
            var state = await grain.Schedule(scheduledAt, cmd, labels, annotations, null);
            return state;
        }

        public async Task<Job<TJobCommand, TResult>?> GetJob<TJobCommand, TResult>(
            string name,
            string @namespace)
            where TJobCommand : ICommand<TResult>
        {
            string key = KeyUtils.BuildJobKey(name, @namespace);
            IJobGrain grain = _client.GetGrain<IJobGrain>(key);
            Job? jobState = await grain.GetState();
            if (jobState is null || jobState.Deleted)
            {
                return null;
            }
            return jobState.Map<TJobCommand, TResult>();
        }

        public Task DeleteJob(string key)
            => _client.GetGrain<IJobGrain>(key).Delete();

        public Task<IEnumerable<Job<TJobCommand, TResult>>> GetJobByLabel<TJobCommand, TResult>(string labelName, string labelValue)
            where TJobCommand : ICommand<TResult>
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Job<TJobCommand, TResult>>> GetJobByLabels<TJobCommand, TResult>(params (string, string)[] labels)
            where TJobCommand : ICommand<TResult>
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Job<TJobCommand, TResult>>> GetJobByCron<TJobCommand, TResult>(string key)
            where TJobCommand : ICommand<TResult>
        {
            throw new NotImplementedException();
        }
    }
}
