// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;

using Fabron.Grains.Job;

using Moq;

using Orleans.TestKit;

using Xunit;

namespace Fabron.Test.Grains.JobReporter
{

    public class JobReporterGrainTests : TestKitBase
    {
        [Fact]
        public async Task Report()
        {
            string jobId = "test_job";
            int version = 1;
            JobState jobState = new(new TestCommand("testtest").ToRaw(), DateTime.UtcNow);
            Mock<IJobReporter>? reporterMock = Silo.AddServiceProbe<IJobReporter>();
            reporterMock.Setup(m => m.Report(jobId, version, jobState))
                .Returns(Task.CompletedTask)
                .Verifiable();

            JobReporterGrain? grain = await Silo.CreateGrainAsync<JobReporterGrain>(jobId);

            await grain.OnJobStateChanged(version, jobState);

            reporterMock.VerifyAll();
        }
    }
}

