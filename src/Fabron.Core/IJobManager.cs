
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Fabron.Contracts;
using Fabron.Mando;

namespace Fabron
{
    public interface IJobManager
    {
        // transient
        Task<Job<TCommand, TResult>> ScheduleJob<TCommand, TResult>(
            string name,
            string @namespace,
            TCommand command,
            DateTimeOffset schedule,
            Dictionary<string, string>? labels = null,
            Dictionary<string, string>? annotations = null)
            where TCommand : ICommand<TResult>;

        Task<Job<TJobCommand, TResult>?> GetJob<TJobCommand, TResult>(string name, string @namespace)
            where TJobCommand : ICommand<TResult>;

        Task<IEnumerable<Job<TJobCommand, TResult>>> GetJobByLabel<TJobCommand, TResult>(string labelName, string labelValue) where TJobCommand : ICommand<TResult>;

        Task<IEnumerable<Job<TJobCommand, TResult>>> GetJobByLabels<TJobCommand, TResult>(params (string, string)[] labels) where TJobCommand : ICommand<TResult>;

        Task<IEnumerable<Job<TJobCommand, TResult>>> GetJobByCron<TJobCommand, TResult>(string cronkey) where TJobCommand : ICommand<TResult>;

        Task DeleteJob(string key);

        // cron
        Task<CronJob<TCommand>> ScheduleCronJob<TCommand>(
            string name,
            string @namespace,
            TCommand command,
            string cronExp,
            DateTimeOffset? notBefore = null,
            DateTimeOffset? expirationTime = null,
            bool suspend = false,
            Dictionary<string, string>? labels = null,
            Dictionary<string, string>? annotations = null) where TCommand : ICommand;

        Task<CronJob<TCommand>?> GetCronJob<TCommand>(string name, string @namespace) where TCommand : ICommand;

        // Task TriggerCronJob(string key);

        // Task SuspendCronJob(string key);
        // Task ResumeCronJob(string key);

        // Task<IEnumerable<CronJob<TJobCommand>>> GetCronJobByLabel<TJobCommand>(string labelName, string labelValue) where TJobCommand : ICommand;

        // Task<IEnumerable<CronJob<TJobCommand>>> GetCronJobByLabels<TJobCommand>(params (string, string)[] labels) where TJobCommand : ICommand;
        // Task DeleteCronJob(string key);
    }
}
