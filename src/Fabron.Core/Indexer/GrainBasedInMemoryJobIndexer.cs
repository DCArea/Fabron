using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabron.Models;
using Orleans;

namespace Fabron.Indexer
{
    public class GrainBasedInMemoryJobIndexer : IJobIndexer, IJobQuerier
    {
        private readonly IGrainFactory _factory;

        public GrainBasedInMemoryJobIndexer(IGrainFactory factory) => _factory = factory;

        private IJobIndexerGrain IndexerGrain => _factory.GetGrain<IJobIndexerGrain>(0);

        public Task Index(Job job) => IndexerGrain.Index(job);
        public Task Index(IEnumerable<Job> jobs) => IndexerGrain.Index(jobs.ToList());
        public Task Index(CronJob job) => IndexerGrain.Index(job);
        public Task Index(IEnumerable<CronJob> jobs) => IndexerGrain.Index(jobs.ToList());
        public Task DeleteCronJob(string key) => IndexerGrain.DeleteCronJob(key);
        public Task DeleteJob(string key) => IndexerGrain.DeleteJob(key);

        public Task<List<CronJob>> GetCronJobByLabel(string labelName, string labelValue)
            => IndexerGrain.GetCronJobByLabel(labelName, labelValue);

        public Task<List<CronJob>> GetCronJobByLabels(IEnumerable<(string, string)> labels)
            => IndexerGrain.GetCronJobByLabels(labels);

        public Task<List<Job>> GetJobByLabel(string labelName, string labelValue)
            => IndexerGrain.GetJobByLabel(labelName, labelValue);

        public Task<List<Job>> GetJobByLabels(IEnumerable<(string, string)> labels)
            => IndexerGrain.GetJobByLabels(labels);

        public Task<Job?> GetJobByKey(string key) => IndexerGrain.GetJobByKey(key);

        public Task<CronJob?> GetCronJobByKey(string key) => IndexerGrain.GetCronJobByKey(key);
    }

    public interface IJobIndexerGrain : IGrainWithIntegerKey
    {
        Task DeleteCronJob(string key);
        Task DeleteJob(string key);
        Task Index(Job job);
        Task Index(List<Job> jobs);
        Task Index(CronJob job);
        Task Index(List<CronJob> job);

        Task<Job?> GetJobByKey(string key);
        Task<List<Job>> GetJobByLabel(string labelName, string labelValue);
        Task<List<Job>> GetJobByLabels(IEnumerable<(string, string)> labels);
        Task<CronJob?> GetCronJobByKey(string key);
        Task<List<CronJob>> GetCronJobByLabel(string labelName, string labelValue);
        Task<List<CronJob>> GetCronJobByLabels(IEnumerable<(string, string)> labels);
    }

    public class JobIndexerGrain : Grain, IJobIndexerGrain
    {
        private readonly InMemoryJobIndexer _indexer = new InMemoryJobIndexer();

        public Task Index(Job job) => _indexer.Index(job);
        public Task Index(List<Job> jobs) => _indexer.Index(jobs);
        public Task Index(CronJob job) => _indexer.Index(job);
        public Task Index(List<CronJob> job) => _indexer.Index(job);
        public Task DeleteJob(string key) => _indexer.DeleteJob(key);
        public Task DeleteCronJob(string key) => _indexer.DeleteCronJob(key);

        public Task<CronJob?> GetCronJobByKey(string key) => _indexer.GetCronJobByKey(key);
        public async Task<List<CronJob>> GetCronJobByLabel(string labelName, string labelValue)
        {
            return await _indexer.GetCronJobByLabel(labelName, labelValue);
        }

        public async Task<List<CronJob>> GetCronJobByLabels(IEnumerable<(string, string)> labels)
        {
            IEnumerable<CronJob>? jobs = await _indexer.GetCronJobByLabels(labels);
            return jobs.ToList();
        }

        public Task<Job?> GetJobByKey(string key) => _indexer.GetJobByKey(key);
        public async Task<List<Job>> GetJobByLabel(string labelName, string labelValue)
        {
            IEnumerable<Job>? jobs = await _indexer.GetJobByLabel(labelName, labelValue);
            return jobs.ToList();
        }

        public async Task<List<Job>> GetJobByLabels(IEnumerable<(string, string)> labels)
        {
            IEnumerable<Job>? jobs = await _indexer.GetJobByLabels(labels);
            return jobs.ToList();
        }
    }
}
