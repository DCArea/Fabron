using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TGH.Contracts;

namespace TGH
{
    public interface IJobManager
    {
        Task<TransientJob<TCommand, TResult>> Schedule<TCommand, TResult>(Guid jobId, TCommand command, DateTime? scheduledAt = null) where TCommand : ICommand<TResult>;
        Task Schedule(Guid jobId, IEnumerable<ICommand> commands);
        Task<CronJob> Schedule<TCommand>(Guid jobId, string cronExp, TCommand command) where TCommand : ICommand;

        Task<TransientJob<TJobCommand, TResult>?> GetJobById<TJobCommand, TResult>(Guid jobId) where TJobCommand : ICommand<TResult>;
        Task<BatchJob?> GetBatchJobById(Guid jobId);
        Task<CronJob?> GetCronJob(Guid jobId);
        Task<CronJobDetail?> GetCronJobDetail(Guid jobId);
    }
}
