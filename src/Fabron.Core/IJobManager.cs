
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
            string key,
            TCommand command,
            DateTime? scheduledAt,
            Dictionary<string, string>? labels,
            Dictionary<string, string>? annotations) where TCommand : ICommand<TResult>;

        Task<Job<TJobCommand, TResult>?> GetJob<TJobCommand, TResult>(string key) where TJobCommand : ICommand<TResult>;

        Task<IEnumerable<Job<TJobCommand, TResult>>> GetJobByLabel<TJobCommand, TResult>(string labelName, string labelValue) where TJobCommand : ICommand<TResult>;

        Task<IEnumerable<Job<TJobCommand, TResult>>> GetJobByLabels<TJobCommand, TResult>(params (string, string)[] labels) where TJobCommand : ICommand<TResult>;

        Task<IEnumerable<Job<TJobCommand, TResult>>> GetJobByCron<TJobCommand, TResult>(string cronkey) where TJobCommand : ICommand<TResult>;

        Task DeleteJob(string key);

        // cron
        Task<CronJob<TCommand>> ScheduleCronJob<TCommand>(
            string cronkey,
            string cronExp,
            TCommand command,
            DateTime? notBefore,
            DateTime? expirationTime,
            bool suspend,
            Dictionary<string, string>? labels,
            Dictionary<string, string>? annotations) where TCommand : ICommand;

        Task TriggerCronJob(string key);

        Task SuspendCronJob(string key);
        Task ResumeCronJob(string key);

        Task<CronJob<TCommand>?> GetCronJob<TCommand>(string cronkey) where TCommand : ICommand;

        Task<IEnumerable<CronJob<TJobCommand>>> GetCronJobByLabel<TJobCommand>(string labelName, string labelValue) where TJobCommand : ICommand;

        Task<IEnumerable<CronJob<TJobCommand>>> GetCronJobByLabels<TJobCommand>(params (string, string)[] labels) where TJobCommand : ICommand;
        Task DeleteCronJob(string key);
    }
}
