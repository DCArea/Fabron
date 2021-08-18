// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Nest;

namespace Fabron
{
    public class ElasticSearchJobQuerier
    {
        private readonly ElasticSearchJobReporterOptions _options;
        private readonly IElasticClient _esClient;

        public ElasticSearchJobQuerier(IOptions<ElasticSearchJobReporterOptions> options, Nest.IElasticClient esClient)
        {
            _options = options.Value;
            _esClient = esClient;
        }
        public async Task GetByLabel(string labelName, string labelValue)
        {
            var res = await _esClient.SearchAsync<JobDocument>(s => s
                .Index(_options.JobIndexName)
                .Query(q => q
                    .Match(m => m.Field($"labels.{labelName}").Query(labelValue))
                )
            );
        }
    }
}
