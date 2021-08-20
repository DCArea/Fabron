// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

using Fabron.Grains.Job;

namespace Fabron
{
    public interface IJobReporter
    {
        Task Report(string jobId, long version, JobState jobState);
    }

    public class NoopJobReporter : IJobReporter
    {
        public Task Report(string jobId, long version, JobState jobState) => Task.CompletedTask;
    }
}
