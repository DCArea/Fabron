
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabron.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;

namespace Fabron.ElasticSearch
{
    public class ElasticSearchJobIndexer : IJobIndexer
    {
        private readonly ILogger<ElasticSearchJobIndexer> _logger;
        private readonly ElasticSearchOptions _options;
        private readonly Nest.IElasticClient _esClient;

        public ElasticSearchJobIndexer(ILogger<ElasticSearchJobIndexer> logger, IOptions<ElasticSearchOptions> options, Nest.IElasticClient esClient)
        {
            _logger = logger;
            _options = options.Value;
            _esClient = esClient;
        }

        public async Task Index(Fabron.Models.Job job)
        {
            JobDocument doc = new(
                job.Metadata.Uid,
                job.Metadata,
                job.Spec,
                job.Status,
                job.Version);
            Nest.IndexResponse res = await _esClient.IndexAsync(doc, idx => idx.Index(_options.JobIndexName));
            if (res.Result == Nest.Result.Error)
            {
                _logger.LogError($"Failed to index doc: {res.DebugInformation}");
            }
        }

        public async Task Index(CronJob job)
        {
            CronJobDocument doc = new(
                job.Metadata.Uid,
                job.Metadata,
                job.Spec,
                job.Status,
                job.Version);
            Nest.IndexResponse res = await _esClient.IndexAsync(doc, idx => idx.Index(_options.CronJobIndexName));
            if (res.Result == Nest.Result.Error)
            {
                _logger.LogError($"Failed to index doc: {res.DebugInformation}");
            }
        }

        public async Task Index(IEnumerable<Fabron.Models.Job> jobs)
        {
            IEnumerable<JobDocument> docs = jobs
                .Select(job => new JobDocument(
                    job.Metadata.Uid,
                    job.Metadata,
                    job.Spec,
                    job.Status,
                    job.Version));
            Nest.BulkResponse res = await Nest.IndexManyExtensions.IndexManyAsync(_esClient, docs, _options.JobIndexName);
            if (res.Errors)
            {
                _logger.LogError($"Failed to index docs: {res.DebugInformation}");
            }
        }

        public async Task Index(IEnumerable<CronJob> jobs)
        {
            IEnumerable<CronJobDocument> docs = jobs
                .Where(job => job is not null)
                .Select(job => new CronJobDocument(job!.Metadata.Uid,
                  job.Metadata,
                  job.Spec,
                  job.Status,
                  job.Version));
            Nest.BulkResponse res = await _esClient.IndexManyAsync(docs, _options.CronJobIndexName);
            if (res.Errors)
            {
                _logger.LogError($"Failed to index docs: {res.DebugInformation}");
            }
        }

        public async Task DeleteJob(string jobId)
        {
            DeleteResponse? res = await _esClient
                .DeleteAsync<JobDocument>(jobId, d => d.Index(_options.JobIndexName));
            if (res.Result == Nest.Result.Error)
            {
                _logger.LogError($"Failed to delete doc: {res.DebugInformation}");
            }
        }

        public async Task DeleteCronJob(string cronJobId)
        {
            DeleteResponse? res = await _esClient
                .DeleteAsync<CronJobDocument>(cronJobId, d => d.Index(_options.CronJobIndexName));
            if (res.Result == Nest.Result.Error)
            {
                _logger.LogError($"Failed to delete doc: {res.DebugInformation}");
            }
        }
    }
}
