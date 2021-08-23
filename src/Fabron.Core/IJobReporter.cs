// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Fabron.Models;

namespace Fabron
{
    public interface IJobReporter
    {
        Task Report(string jobId, ulong version, Job job);
        Task Report(string jobId, ulong version, CronJob job);
    }

    public class NoopJobReporter : IJobReporter
    {
        public Task Report(string jobId, ulong version, Job job) => Task.CompletedTask;
        public Task Report(string jobId, ulong version, CronJob job) => Task.CompletedTask;
    }
}
