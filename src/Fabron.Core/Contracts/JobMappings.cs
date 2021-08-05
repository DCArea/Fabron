// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Text.Json;

using Fabron.Grains.BatchJob;
using Fabron.Grains.CronJob;
//using Fabron.Grains.CronJob;
using Fabron.Grains.Job;
using Fabron.Mando;

namespace Fabron.Contracts
{
    public static class JobMappings
    {
        public static Job<TCommand, TResult> Map<TCommand, TResult>(this JobState jobState)
            where TCommand : ICommand<TResult>
        {
            TCommand? cmdData = JsonSerializer.Deserialize<TCommand>(jobState.Command.Data);
            if (cmdData is null)
            {
                throw new Exception();
            }

            TResult? cmdResult = jobState.Command.Result is null
                    ? default
                    : JsonSerializer.Deserialize<TResult>(jobState.Command.Result);

            Job<TCommand, TResult> job = new(
                new(cmdData, cmdResult),
                jobState.CreatedAt,
                jobState.ScheduledAt,
                jobState.StartedAt,
                jobState.FinishedAt,
                (JobStatus)((int)jobState.Status - 1),
                jobState.Reason
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

        public static CronJob Map<TCommand>(this CronJobState jobState)
            where TCommand : ICommand
        {
            TCommand? cmdData = JsonSerializer.Deserialize<TCommand>(jobState.Command.Data);
            if (cmdData is null)
            {
                throw new Exception();
            }

            return jobState.Map(cmdData);
        }
        public static CronJob Map(this CronJobState jobState, CommandRegistry registry)
        {
            string cmdName = jobState.Command.Name;
            ICommand cmdData = (ICommand)JsonSerializer.Deserialize(jobState.Command.Data, registry.CommandTypeRegistrations[cmdName])!;
            if (cmdData is null)
            {
                throw new InvalidOperationException();
            }

            return jobState.Map(cmdData);
        }

        private static CronJob Map(this CronJobState jobState, ICommand cmd)
        {
            CronJob job = new(
                jobState.CronExp,
                cmd,
                jobState.PendingJobs.Select(job => job.To()),
                jobState.ScheduledJobs.Select(job => job.To()),
                jobState.FinishedJobs.Select(job => job.To()),
                (JobStatus)(int)jobState.Status,
                jobState.Reason
            );
            return job;
        }

        public static CronChildJob To(this CronJobStateChild childJobState)
        {
            CronChildJob childJob = new(
                childJobState.Id,
                (JobStatus)(int)childJobState.Status,
                childJobState.ScheduledAt
            );
            return childJob;
        }
    }

}
