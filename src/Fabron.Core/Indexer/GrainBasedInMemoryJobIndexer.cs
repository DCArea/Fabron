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
        public Task DeleteCronJob(string cronJobId) => IndexerGrain.DeleteCronJob(cronJobId);
        public Task DeleteJob(string jobId) => IndexerGrain.DeleteJob(jobId);

        public async Task<IEnumerable<CronJob>> GetCronJobByLabel(string labelName, string labelValue)
        {
            List<CronJob>? jobs = await IndexerGrain.GetCronJobByLabel(labelName, labelValue);
            return jobs;
        }

        public async Task<IEnumerable<CronJob>> GetCronJobByLabels(IEnumerable<(string, string)> labels)
        {
            List<CronJob>? jobs = await IndexerGrain.GetCronJobByLabels(labels);
            return jobs;
        }

        public async Task<IEnumerable<Job>> GetJobByLabel(string labelName, string labelValue)
        {
            List<Job>? jobs = await IndexerGrain.GetJobByLabel(labelName, labelValue);
            return jobs;
        }

        public async Task<IEnumerable<Job>> GetJobByLabels(IEnumerable<(string, string)> labels)
        {
            List<Job> jobs = await IndexerGrain.GetJobByLabels(labels);
            return jobs;
        }
    }

    public interface IJobIndexerGrain : IGrainWithIntegerKey
    {
        Task DeleteCronJob(string cronJobId);
        Task DeleteJob(string jobId);
        Task Index(Job job);
        Task Index(List<Job> jobs);
        Task Index(CronJob job);
        Task Index(List<CronJob> job);

        Task<List<Job>> GetJobByLabel(string labelName, string labelValue);
        Task<List<Job>> GetJobByLabels(IEnumerable<(string, string)> labels);
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
        public Task DeleteJob(string jobId) => _indexer.DeleteJob(jobId);
        public Task DeleteCronJob(string cronJobId) => _indexer.DeleteCronJob(cronJobId);

        public async Task<List<CronJob>> GetCronJobByLabel(string labelName, string labelValue)
        {
            IEnumerable<CronJob>? jobs = await _indexer.GetCronJobByLabel(labelName, labelValue);
            return jobs.ToList();
        }

        public async Task<List<CronJob>> GetCronJobByLabels(IEnumerable<(string, string)> labels)
        {
            IEnumerable<CronJob>? jobs = await _indexer.GetCronJobByLabels(labels);
            return jobs.ToList();
        }

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
