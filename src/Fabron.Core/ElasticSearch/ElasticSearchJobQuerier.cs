
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Nest;

namespace Fabron.ElasticSearch
{
    public class ElasticSearchJobQuerier : IJobQuerier
    {
        private readonly ElasticSearchOptions _options;
        private readonly ILogger<ElasticSearchJobQuerier> _logger;
        private readonly IElasticClient _esClient;

        public ElasticSearchJobQuerier(
            ILogger<ElasticSearchJobQuerier> logger,
            IOptions<ElasticSearchOptions> options,
            IElasticClient esClient)
        {
            _options = options.Value;
            _logger = logger;
            _esClient = esClient;
        }

        public async Task<Models.Job?> GetJobByKey(string key)
        {
            ISearchResponse<JobDocument> res = await SearchByKeyAsync<JobDocument>(_options.JobIndexName, key);
            return res.Documents.Select(doc => doc.ToResource()).FirstOrDefault();
        }

        public async Task<List<Models.Job>> GetJobByLabel(string labelName, string labelValue)
        {
            ISearchResponse<JobDocument> res = await SearchByLabelAsync<JobDocument>(_options.JobIndexName, labelName, labelValue);
            return res.Documents.Select(doc => doc.ToResource()).ToList();
        }

        public async Task<List<Models.Job>> GetJobByLabels(IEnumerable<(string, string)> labels)
        {
            ISearchResponse<JobDocument> res = await SearchByLabelsAsync<JobDocument>(_options.JobIndexName, labels);
            return res.Documents.Select(doc => doc.ToResource()).ToList();
        }

        public async Task<Models.CronJob?> GetCronJobByKey(string key)
        {
            ISearchResponse<CronJobDocument> res = await SearchByKeyAsync<CronJobDocument>(_options.CronJobIndexName, key);
            return res.Documents.Select(doc => doc.ToResource()).FirstOrDefault();
        }

        public async Task<List<Models.CronJob>> GetCronJobByLabel(string labelName, string labelValue)
        {
            ISearchResponse<CronJobDocument> res = await SearchByLabelAsync<CronJobDocument>(_options.CronJobIndexName, labelName, labelValue);
            return res.Documents.Select(doc => doc.ToResource()).ToList();
        }

        public async Task<List<Models.CronJob>> GetCronJobByLabels(IEnumerable<(string, string)> labels)
        {
            ISearchResponse<CronJobDocument> res = await SearchByLabelsAsync<CronJobDocument>(_options.CronJobIndexName, labels);
            return res.Documents.Select(doc => doc.ToResource()).ToList();
        }

        private async Task<ISearchResponse<TDocument>> SearchByKeyAsync<TDocument>(string indexName, string key)
            where TDocument : class
        {
            ISearchResponse<TDocument> res = await _esClient.SearchAsync<TDocument>(s => s
                .Index(indexName)
                .Query(q => q
                    .Term(m => m.Field("metadata.key.keyword").Value(key))
                )
            );
            return res;
        }

        private async Task<ISearchResponse<TDocument>> SearchByLabelAsync<TDocument>(string indexName, string labelName, string labelValue)
            where TDocument : class
        {
            labelName = labelName.ToNormalized();
            ISearchResponse<TDocument> res = await _esClient.SearchAsync<TDocument>(s => s
                .Index(indexName)
                .Query(q => q
                    .Term(m => m.Field($"metadata.labels.{labelName}.keyword").Value(labelValue))
                )
            );
            return res;
        }

        private async Task<ISearchResponse<TDocument>> SearchByLabelsAsync<TDocument>(string indexName, IEnumerable<(string, string)> labels)
            where TDocument : class
        {
            var must = labels
                .Select<(string, string), Func<QueryContainerDescriptor<TDocument>, QueryContainer>>(label => (QueryContainerDescriptor<TDocument> d)
                    => d.Term(t => t.Field($"metadata.labels.{label.Item1.ToNormalized()}.keyword").Value(label.Item2)));
            ISearchResponse<TDocument> res = await _esClient.SearchAsync<TDocument>(s => s
                .Index(_options.JobIndexName)
                .Query(q => q
                    .Bool(b => b.Must(must))
                )
            );
            return res;
        }
    }
}
