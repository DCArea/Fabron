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
            _jobs[job.Metadata.Key] = job;
            return Task.CompletedTask;
        }

        public Task Index(IEnumerable<Job> jobs)
        {
            foreach (Job? job in jobs)
            {
                _jobs[job.Metadata.Key] = job;
            }
            return Task.CompletedTask;
        }

        public Task Index(CronJob job)
        {
            _cronJobs[job.Metadata.Key] = job;
            return Task.CompletedTask;
        }

        public Task Index(IEnumerable<CronJob> jobs)
        {
            foreach (CronJob? job in jobs)
            {
                _cronJobs[job.Metadata.Key] = job;
            }
            return Task.CompletedTask;
        }

        public Task DeleteJob(string key)
        {
            _jobs.Remove(key);
            return Task.CompletedTask;
        }

        public Task DeleteCronJob(string key)
        {
            _cronJobs.Remove(key);
            return Task.CompletedTask;
        }

        public Task<List<CronJob>> GetCronJobByLabel(string labelName, string labelValue)
        {
            IEnumerable<CronJob> jobs = _cronJobs.Values
                .Where(v => v.Metadata.Labels.TryGetValue(labelName, out string? value) && value == labelValue);
            return Task.FromResult(jobs.ToList());
        }

        public Task<List<CronJob>> GetCronJobByLabels(IEnumerable<(string, string)> labels)
        {
            IEnumerable<CronJob> jobs = _cronJobs.Values;
            foreach ((string labelName, string labelValue) in labels)
            {
                jobs = jobs.Where(v => v.Metadata.Labels.TryGetValue(labelName, out string? value) && value == labelValue);

            }
            return Task.FromResult(jobs.ToList());
        }

        public Task<List<Job>> GetJobByLabel(string labelName, string labelValue)
        {
            IEnumerable<Job> jobs = _jobs.Values
                .Where(v => v.Metadata.Labels.TryGetValue(labelName, out string? value) && value == labelValue);
            return Task.FromResult(jobs.ToList());
        }

        public Task<List<Job>> GetJobByLabels(IEnumerable<(string, string)> labels)
        {
            IEnumerable<Job> jobs = _jobs.Values;
            foreach ((string labelName, string labelValue) in labels)
            {
                jobs = jobs.Where(v => v.Metadata.Labels.TryGetValue(labelName, out string? value) && value == labelValue);

            }
            return Task.FromResult(jobs.ToList());
        }

        public Task<Job?> GetJobByKey(string key)
        {
            return Task.FromResult(_jobs.TryGetValue(key, out Job? job) ? job : null);
        }
        public Task<CronJob?> GetCronJobByKey(string key)
        {
            return Task.FromResult(_cronJobs.TryGetValue(key, out CronJob? job) ? job : null);
        }
    }
}
