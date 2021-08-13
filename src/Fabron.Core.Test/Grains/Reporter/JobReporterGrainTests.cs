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
            JobState jobState = new()
            {
                Metadata = new JobMetadata(jobId, DateTime.Now, new()),
                Spec = new JobSpec(DateTime.Now, "test", "test"),
            };
            Mock<IJobReporter>? reporterMock = Silo.AddServiceProbe<IJobReporter>();
            reporterMock.Setup(m => m.Report(jobId, jobState.Metadata.ResourceVersion, jobState))
                .Returns(Task.CompletedTask)
                .Verifiable();

            JobReporterGrain? grain = await Silo.CreateGrainAsync<JobReporterGrain>(jobId);

            await grain.OnJobStateChanged(jobState);

            reporterMock.VerifyAll();
        }
    }
}

