// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Text.Json;

using Fabron.Grains.BatchJob;
using Fabron.Grains.CronJob;
using Fabron.Grains.Job;
using Fabron.Mando;

namespace Fabron.Contracts
{
    public static class JobMappings
    {
        public static Job<TCommand, TResult> Map<TCommand, TResult>(this JobState jobState)
            where TCommand : ICommand<TResult>
        {
            TCommand? cmdData = JsonSerializer.Deserialize<TCommand>(jobState.Spec.CommandData);
            if (cmdData is null)
            {
                throw new Exception();
            }

            TResult? cmdResult = jobState.Status.Result is null
                    ? default
                    : JsonSerializer.Deserialize<TResult>(jobState.Status.Result);

            Job<TCommand, TResult> job = new(
                new(cmdData, cmdResult),
                jobState.Metadata.CreationTimestamp,
                jobState.Spec.Schedule,
                jobState.Status.StartedAt,
                jobState.Status.FinishedAt,
                (JobStatus)((int)jobState.Status.ExecutionStatus - 1),
                jobState.Status.Reason
            );
            return job;
        }

        public static BatchJob Map(this BatchJobState jobState, CommandRegistry registry) => new(
                jobState.PendingJobs.Select(j => j.Map(registry)),
                jobState.FinishedJobs.Select(j => j.Map(registry)),
                (JobStatus)(int)jobState.Status,
                jobState.Reason
            );

        public static BatchChildJob Map(this BatchJobStateChild childJobState, CommandRegistry registry)
        {
            string? cmdName = childJobState.Command.Name;
            object? cmdData = JsonSerializer.Deserialize(childJobState.Command.Data, registry.CommandTypeRegistrations[cmdName])!;
            BatchChildJob childJob = new(
                childJobState.Id,
                cmdData
            );
            return childJob;
        }

        public static CronJob<TCommand> Map<TCommand>(this CronJobState jobState)
            where TCommand : ICommand
        {
            TCommand? cmdData = JsonSerializer.Deserialize<TCommand>(jobState.Spec.CommandData);
            if (cmdData is null)
            {
                throw new InvalidOperationException();
            }

            return jobState.Map<TCommand>(cmdData);
        }
        public static CronJob Map(this CronJobState jobState, CommandRegistry registry)
        {
            string cmdName = jobState.Spec.CommandName;
            ICommand cmdData = (ICommand)JsonSerializer.Deserialize(jobState.Spec.CommandData, registry.CommandTypeRegistrations[cmdName])!;
            if (cmdData is null)
            {
                throw new InvalidOperationException();
            }

            return jobState.Map(cmdData);
        }

        private static CronJob<TCommand> Map<TCommand>(this CronJobState jobState, TCommand cmd)
            where TCommand : ICommand
        {
            CronJob<TCommand> job = new(
                jobState.Spec.Schedule,
                cmd,
                jobState.Status.Jobs.Select(job => job.To()),
                jobState.Status.Reason
            );
            return job;
        }

        private static CronJob Map(this CronJobState jobState, ICommand cmd)
        {
            CronJob job = new(
                jobState.Spec.Schedule,
                cmd,
                jobState.Status.Jobs.Select(job => job.To()),
                jobState.Status.Reason
            );
            return job;
        }

        public static CronChildJob To(this JobItem childJobState)
        {
            CronChildJob childJob = new(
                childJobState.Uid,
                childJobState.Status,
                childJobState.Schedule
            );
            return childJob;
        }
    }

}
