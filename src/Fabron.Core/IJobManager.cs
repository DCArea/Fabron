// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Fabron.Contracts;
using Fabron.Mando;

namespace Fabron
{
    public interface IJobManager
    {
        Task<Job<TCommand, TResult>> Schedule<TCommand, TResult>(string jobId, TCommand command, DateTime? scheduledAt = null, Dictionary<string, string>? labels = null) where TCommand : ICommand<TResult>;
        Task<CronJob<TCommand>> Schedule<TCommand>(string jobId, string cronExp, TCommand command, Dictionary<string, string>? labels) where TCommand : ICommand;

        Task<Job<TJobCommand, TResult>?> GetJobById<TJobCommand, TResult>(string jobId) where TJobCommand : ICommand<TResult>;
        Task<CronJob<TCommand>?> GetCronJob<TCommand>(string jobId) where TCommand: ICommand;
    }
}
