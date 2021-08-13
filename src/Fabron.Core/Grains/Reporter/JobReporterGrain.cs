// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Orleans;
using Orleans.Concurrency;

namespace Fabron.Grains.Job
{
    public interface IJobReporterGrain : IGrainWithStringKey
    {
        [OneWay]
        Task OnJobStateChanged(JobState jobState);

        Task OnJobFinalized(JobState jobState);
    }

    public class JobReporterGrain : Grain, IJobReporterGrain
    {
        private readonly ILogger<JobReporterGrain> _logger;
        private readonly IJobReporter _reporter;

        public JobReporterGrain(ILogger<JobReporterGrain> logger, IJobReporter reporter)
        {
            _logger = logger;
            _reporter = reporter;
        }

        public async Task OnJobStateChanged(JobState jobState)
        {
            await _reporter.Report(this.GetPrimaryKeyString(), jobState.Metadata.ResourceVersion, jobState);
            return;
        }

        public async Task OnJobFinalized(JobState jobState)
        {
            await _reporter.Report(this.GetPrimaryKeyString(), jobState.Metadata.ResourceVersion, jobState);
            return;
        }
    }
}
