// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

using Fabron;
using Fabron.Grains.Job;

namespace FabronService.FunctionalTests
{
    public class TestJobReporter : IJobReporter
    {
        public Task Report(string jobId, long version, JobState jobState) => Task.CompletedTask;
    }
}
