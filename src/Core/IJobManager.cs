using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron.Contracts;

namespace Fabron
{
    public interface IJobManager
    {
        Task<TransientJob<TCommand, TResult>> Schedule<TCommand, TResult>(string jobId, TCommand command, DateTime? scheduledAt = null) where TCommand : ICommand<TResult>;
        Task Schedule(string jobId, IEnumerable<ICommand> commands);
        Task<CronJob> Schedule<TCommand>(string jobId, string cronExp, TCommand command) where TCommand : ICommand;

        Task<TransientJob<TJobCommand, TResult>?> GetJobById<TJobCommand, TResult>(string jobId) where TJobCommand : ICommand<TResult>;
        Task<BatchJob?> GetBatchJobById(string jobId);
        Task<CronJob?> GetCronJob(string jobId);
        Task<CronJobDetail?> GetCronJobDetail(string jobId);
    }
}
