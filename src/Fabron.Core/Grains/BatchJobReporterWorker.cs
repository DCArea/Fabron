// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using Prometheus;

namespace Fabron.Grains
{
    public interface IBatchJobReporterWorker : IGrainWithIntegerKey
    {
        Task ReportJob(string jobId);
        Task ReportCronJob(string cronJobId);
    }

    [StatelessWorker]
    [Reentrant]
    public class BatchJobReporterWorker : Grain, IBatchJobReporterWorker
    {
        public static readonly Counter ReportedJobCount = Metrics
            .CreateCounter("fabron_jobs_reported_total", "Number of reported jobs.");
        public static readonly Histogram JobIndexDuration = Metrics
            .CreateHistogram("fabron_job_index_duration_seconds", "");

        private readonly BatchWorkerFromDelegate _worker;
        private readonly List<string> _pendingJobs;
        private readonly List<string> _pendingCronJobs;
        private readonly ILogger _logger;
        private readonly IJobReporter _reporter;

        public BatchJobReporterWorker(ILogger<BatchJobReporterWorker> logger, IJobReporter reporter)
        {
            _worker = new BatchWorkerFromDelegate(Submit);
            _pendingJobs = new List<string>();
            _pendingCronJobs = new List<string>();
            _logger = logger;
            _reporter = reporter;
        }

        public Task ReportJob(string jobId)
        {
            _pendingJobs.Add(jobId);
            _worker.Notify();
            return _worker.WaitForCurrentWorkToBeServiced();
        }

        public Task ReportCronJob(string cronJobId)
        {
            _pendingCronJobs.Add(cronJobId);
            _worker.Notify();
            return _worker.WaitForCurrentWorkToBeServiced();
        }

        public Task Submit() => Task.WhenAll(IndexJobs(), IndexCronJobs());

        private async Task IndexJobs()
        {
            if (_pendingJobs.Count == 0)
            {
                return;
            }

            string[] currentBatch = _pendingJobs.ToArray();
            IEnumerable<string> ids = currentBatch.GroupBy(id => id).Select(g => g.Key);
            Fabron.Models.Job?[] jobStates = await Task.WhenAll(ids.Select(jobId => GrainFactory.GetGrain<IJobGrain>(jobId).GetState()));
            List<Models.Job> jobs = jobStates
                .Where(job => job is not null)
                .Cast<Fabron.Models.Job>()
                .ToList();

            using (JobIndexDuration.NewTimer())
            {
                await _reporter.Report(jobs);
            }

            _pendingJobs.RemoveRange(0, currentBatch.Length);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"Jobs[Reported]: {jobs.Count}");
                _logger.LogDebug($"Jobs[Pending]: {_pendingJobs.Count}");
            }
        }

        private async Task IndexCronJobs()
        {
            if (_pendingCronJobs.Count == 0)
            {
                return;
            }

            string[]? currentBatch = _pendingCronJobs.ToArray();
            IEnumerable<string> ids = currentBatch.GroupBy(id => id).Select(g => g.Key);
            Fabron.Models.CronJob?[] jobStates = await Task.WhenAll(ids.Select(jobId => GrainFactory.GetGrain<ICronJobGrain>(jobId).GetState()));
            List<Models.CronJob> jobs = jobStates
                .Where(job => job is not null)
                .Cast<Fabron.Models.CronJob>()
                .ToList();

            using (JobIndexDuration.NewTimer())
            {
                await _reporter.Report(jobs);
            }

            _pendingCronJobs.RemoveRange(0, currentBatch.Length);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"Jobs[Reported]: {jobs.Count}");
                _logger.LogDebug($"Jobs[Pending]: {_pendingCronJobs.Count}");
            }
        }
    }
}
