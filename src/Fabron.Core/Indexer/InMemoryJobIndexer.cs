using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabron.Models;

namespace Fabron.Indexer
{
    public class InMemoryJobIndexer : IJobIndexer, IJobQuerier
    {
        public readonly Dictionary<string, Job> _jobs = new();
        public readonly Dictionary<string, CronJob> _cronJobs = new();
        public Task Index(Job job)
        {
            _jobs[job.Metadata.Uid] = job;
            return Task.CompletedTask;
        }

        public Task Index(IEnumerable<Job> jobs)
        {
            foreach (Job? job in jobs)
            {
                _jobs[job.Metadata.Uid] = job;
            }
            return Task.CompletedTask;
        }

        public Task Index(CronJob job)
        {
            _cronJobs[job.Metadata.Uid] = job;
            return Task.CompletedTask;
        }

        public Task Index(IEnumerable<CronJob> jobs)
        {
            foreach (CronJob? job in jobs)
            {
                _cronJobs[job.Metadata.Uid] = job;
            }
            return Task.CompletedTask;
        }

        public Task DeleteJob(string jobId)
        {
            _jobs.Remove(jobId);
            return Task.CompletedTask;
        }

        public Task DeleteCronJob(string cronJobId)
        {
            _cronJobs.Remove(cronJobId);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<CronJob>> GetCronJobByLabel(string labelName, string labelValue)
        {
            IEnumerable<CronJob> jobs = _cronJobs.Values
                .Where(v => v.Metadata.Labels.TryGetValue(labelName, out string? value) && value == labelValue);
            return Task.FromResult(jobs);
        }

        public Task<IEnumerable<CronJob>> GetCronJobByLabels(IEnumerable<(string, string)> labels)
        {
            IEnumerable<CronJob> jobs = _cronJobs.Values;
            foreach ((string labelName, string labelValue) in labels)
            {
                jobs = jobs.Where(v => v.Metadata.Labels.TryGetValue(labelName, out string? value) && value == labelValue);

            }
            return Task.FromResult(jobs);
        }

        public Task<IEnumerable<Job>> GetJobByLabel(string labelName, string labelValue)
        {
            IEnumerable<Job> jobs = _jobs.Values
                .Where(v => v.Metadata.Labels.TryGetValue(labelName, out string? value) && value == labelValue);
            return Task.FromResult(jobs);
        }

        public Task<IEnumerable<Job>> GetJobByLabels(IEnumerable<(string, string)> labels)
        {
            IEnumerable<Job> jobs = _jobs.Values;
            foreach ((string labelName, string labelValue) in labels)
            {
                jobs = jobs.Where(v => v.Metadata.Labels.TryGetValue(labelName, out string? value) && value == labelValue);

            }
            return Task.FromResult(jobs);
        }
    }
}
