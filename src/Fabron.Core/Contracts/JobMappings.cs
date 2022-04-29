using System;
using System.Text.Json;
using Fabron.Contracts;
using Fabron.Mando;
using Fabron.Models;

namespace Fabron
{
    public static class JobMappings
    {
        public static Job<TCommand, TResult> Map<TCommand, TResult>(this Job job)
            where TCommand : ICommand<TResult>
        {
            TCommand? cmdData = JsonSerializer.Deserialize<TCommand>(job.Spec.Command.Data)
                ?? throw new InvalidOperationException();

            TResult? cmdResult = job.Status.Result is null
                    ? default
                    : JsonSerializer.Deserialize<TResult>(job.Status.Result);

            Job<TCommand, TResult> typedJob = new(
                job.Metadata,
                new JobSpec<TCommand>(
                    new(
                        job.Spec.Command.Name,
                        cmdData
                    ),
                    job.Spec.Schedule
                ),
                new JobStatus<TResult>(
                    job.Status.ExecutionStatus,
                    cmdResult,
                    job.Status.Reason,
                    job.Status.Message)
            );
            return typedJob;
        }

        public static CronJob<TCommand> Map<TCommand>(this Models.CronJob cronJob)
            where TCommand : ICommand
        {
            TCommand? cmdData = JsonSerializer.Deserialize<TCommand>(cronJob.Spec.Command.Data)
                ?? throw new InvalidOperationException();

            CronJob<TCommand> typedCronJob = new(
                cronJob.Metadata,
                new CronJobSpec<TCommand>(
                    new CommandSpec<TCommand>(
                        cronJob.Spec.Command.Name,
                        cmdData
                    ),
                    cronJob.Spec.Schedule,
                    cronJob.Spec.NotBefore,
                    cronJob.Spec.ExpirationTime,
                    cronJob.Spec.Suspend),
                cronJob.Status
            );
            return typedCronJob;
        }
    }
}
