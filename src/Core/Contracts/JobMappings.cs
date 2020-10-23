using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TGH.Grains.BatchJob;
using TGH.Grains.TransientJob;
using TGH.Services;

namespace TGH.Contracts
{
    public static class JobMappings
    {
        public static TransientJob<TCommand, TResult> To<TCommand, TResult>(this TransientJobState jobState)
            where TCommand : ICommand<TResult>
        {
            TCommand cmdData = JsonSerializer.Deserialize<TCommand>(jobState.Command.Data);
            if (cmdData is null)
                throw new Exception();
            TResult? cmdResult = jobState.Command.Result is null
                    ? default
                    : JsonSerializer.Deserialize<TResult>(jobState.Command.Result);

            var job = new TransientJob<TCommand, TResult>(
                new(cmdData, cmdResult),
                jobState.CreatedAt,
                jobState.ScheduledAt,
                jobState.StartedAt,
                jobState.FinishedAt,
                JobStatus.Created,
                null
            );
            return job;
        }

        public static BatchJob To(this BatchJobState jobState, CommandRegistry registry)
        {
            return new BatchJob(
                jobState.PendingJobs.Select(j => j.To(registry)),
                jobState.FinishedJobs.Select(j => j.To(registry)),
                (JobStatus)(int)jobState.Status,
                jobState.Reason
            );
        }

        public static ChildJob To(this ChildJobState childJobState, CommandRegistry registry)
        {
            string? cmdName = childJobState.Command.Name;
            object? cmdData = JsonSerializer.Deserialize(childJobState.Command.Data, registry.CommandTypeRegistrations[cmdName])!;
            ChildJob childJob = new ChildJob(
                childJobState.Id,
                cmdData
            );
            return childJob;
        }
    }

}
