
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabron.ElasticSearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly ElasticSearchOptions _options;
        private readonly ILogger _logger;
        private readonly Nest.IElasticClient _esClient;

        public BatchJobReporterWorker(ILogger<BatchJobReporterWorker> logger, IOptions<ElasticSearchOptions> options, Nest.IElasticClient esClient)
        {
            _worker = new BatchWorkerFromDelegate(Submit);
            _pendingJobs = new List<string>();
            _pendingCronJobs = new List<string>();
            _options = options.Value;
            _logger = logger;
            _esClient = esClient;
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

            string[]? currentBatch = _pendingJobs.ToArray();
            IEnumerable<string> ids = currentBatch.GroupBy(id => id).Select(g => g.Key);
            Fabron.Models.Job?[] jobs = await Task.WhenAll(ids.Select(jobId => GrainFactory.GetGrain<IJobGrain>(jobId).GetState()));

            IEnumerable<JobDocument> docs = jobs.Where(job => job is not null).Select(job => new JobDocument(job!.Metadata.Uid,
                  job.Metadata,
                  job.Spec,
                  job.Status));
            Nest.BulkResponse res;
            using (JobIndexDuration.NewTimer())
            {
                res = await Nest.IndexManyExtensions.IndexManyAsync(_esClient, docs, _options.JobIndexName);
            }
            _logger.LogDebug($"Indexed: {res.Items.Count}");

            if (res.Errors)
            {
                _logger.LogError($"Failed to index docs: {res.ItemsWithErrors}");
            }

            _pendingJobs.RemoveRange(0, currentBatch.Length);
            _logger.LogDebug($"Pending: {_pendingJobs.Count}");
        }

        private async Task IndexCronJobs()
        {
            if (_pendingCronJobs.Count == 0)
            {
                return;
            }

            string[]? currentBatch = _pendingCronJobs.ToArray();
            IEnumerable<string> ids = currentBatch.GroupBy(id => id).Select(g => g.Key);
            Fabron.Models.CronJob?[] jobs = await Task.WhenAll(ids.Select(jobId => GrainFactory.GetGrain<ICronJobGrain>(jobId).GetState()));

            IEnumerable<CronJobDocument> docs = jobs.Where(job => job is not null).Select(job => new CronJobDocument(job!.Metadata.Uid,
                  job.Metadata,
                  job.Spec,
                  job.Status));

            Nest.BulkResponse res = await Nest.IndexManyExtensions.IndexManyAsync(_esClient, docs, _options.CronJobIndexName);
            if (res.Errors)
            {
                _logger.LogError($"Failed to index docs: {res.ItemsWithErrors}");
            }
            _pendingCronJobs.RemoveRange(0, currentBatch.Length);
        }
    }
}
