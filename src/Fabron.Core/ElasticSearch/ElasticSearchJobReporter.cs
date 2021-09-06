// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabron.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fabron.ElasticSearch
{
    public class ElasticSearchJobReporter : IJobReporter
    {
        private readonly ILogger<ElasticSearchJobReporter> _logger;
        private readonly ElasticSearchOptions _options;
        private readonly Nest.IElasticClient _esClient;

        public ElasticSearchJobReporter(ILogger<ElasticSearchJobReporter> logger, IOptions<ElasticSearchOptions> options, Nest.IElasticClient esClient)
        {
            _logger = logger;
            _options = options.Value;
            _esClient = esClient;
        }

        public async Task Report(Job job)
        {
            JobDocument doc = new(job.Metadata.Uid,
                job.Metadata,
                job.Spec,
                job.Status);
            Nest.IndexResponse res = await _esClient.IndexAsync(doc, idx => idx.Index(_options.JobIndexName));
            if (res.Result == Nest.Result.Error)
            {
                _logger.LogError($"Failed to index doc: {res.DebugInformation}");
            }
        }

        public async Task Report(CronJob job)
        {
            CronJobDocument doc = new(job.Metadata.Uid,
                job.Metadata,
                job.Spec,
                job.Status);
            Nest.IndexResponse res = await _esClient.IndexAsync(doc, idx => idx.Index(_options.CronJobIndexName));
            if (res.Result == Nest.Result.Error)
            {
                _logger.LogError($"Failed to index doc: {res.DebugInformation}");
            }
        }

        public async Task Report(IEnumerable<Job> jobs)
        {
            IEnumerable<JobDocument> docs = jobs
                .Where(job => job is not null)
                .Select(job => new JobDocument(job!.Metadata.Uid,
                  job.Metadata,
                  job.Spec,
                  job.Status));
            Nest.BulkResponse res = await Nest.IndexManyExtensions.IndexManyAsync(_esClient, docs, _options.JobIndexName);
            if (res.Errors)
            {
                _logger.LogError($"Failed to index docs: {res.DebugInformation}");
            }
        }

        public async Task Report(IEnumerable<CronJob> jobs)
        {
            IEnumerable<CronJobDocument> docs = jobs
                .Where(job => job is not null)
                .Select(job => new CronJobDocument(job!.Metadata.Uid,
                  job.Metadata,
                  job.Spec,
                  job.Status));
            Nest.BulkResponse res = await Nest.IndexManyExtensions.IndexManyAsync(_esClient, docs, _options.CronJobIndexName);
            if (res.Errors)
            {
                _logger.LogError($"Failed to index docs: {res.DebugInformation}");
            }
        }
    }
}
