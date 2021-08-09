// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;

using Fabron.Grains;
using Fabron.Grains.Job;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fabron
{
    public record JobDocument(
        string Id,
        string CommandName,
        DateTime CreatedAt,
        DateTime Schedule,
        DateTime? FinishedAt,
        string? Reason,
        ExecutionStatus Status,
        bool Finalized,
        long Version
        );

    public class ElasticSearchJobReporter : IJobReporter
    {
        private readonly ILogger<ElasticSearchJobReporter> _logger;
        private readonly ElasticSearchJobReporterOptions _options;
        private readonly Nest.IElasticClient _esClient;

        public ElasticSearchJobReporter(ILogger<ElasticSearchJobReporter> logger, IOptions<ElasticSearchJobReporterOptions> options, Nest.IElasticClient esClient)
        {
            _logger = logger;
            _options = options.Value;
            _esClient = esClient;
        }

        public async Task Report(string jobId, long version, JobState jobState)
        {
            JobDocument doc = new(jobId, jobState.Spec.CommandName, jobState.CreatedAt, jobState.Spec.Schedule, jobState.Status.FinishedAt, jobState.Status.Reason, jobState.Status.ExecutionStatus, jobState.Status.Finalized, version);
            Nest.IndexResponse res = await _esClient.IndexAsync(doc, idx => idx.Index(_options.JobIndexName));
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(res.DebugInformation);
            }
            if (res.Result == Nest.Result.Error)
            {
                _logger.LogWarning($"Failed to index doc: {res.DebugInformation}");
            }
        }
    }
}
