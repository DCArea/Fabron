using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron.Models;

namespace Fabron.Store;

public interface IJobStore
{
    Task SaveAsync(Job job);
    Task<Job?> FindAsync(string name, string @namespace);
    Task DeleteAsync(string name, string @namespace);
}

public interface ICronJobStore
{
    Task SaveAsync(CronJob job);
    Task<CronJob?> FindAsync(string name, string @namespace);
    Task DeleteAsync(string name, string @namespace);
}

public class InMemoryJobStore : IJobStore, ICronJobStore
{
    private readonly Dictionary<string, Job> _jobs = new();
    private readonly Dictionary<string, CronJob> _cronJobs = new();

    Task IJobStore.SaveAsync(Job job)
    {
        string key = job.Metadata.Namespace + '/' + job.Metadata.Name;
        _jobs[key] = job;
        return Task.CompletedTask;
    }

    Task<Job?> IJobStore.FindAsync(string name, string @namespace)
    {
        string key = @namespace + '/' + name;
        return Task.FromResult(_jobs.TryGetValue(key, out var job) ? job : null);
    }

    Task IJobStore.DeleteAsync(string name, string @namespace)
    {
        string key = @namespace + '/' + name;
        _jobs.Remove(key);
        return Task.CompletedTask;
    }

    Task ICronJobStore.SaveAsync(CronJob job)
    {
        string key = job.Metadata.Namespace + '/' + job.Metadata.Name;
        _cronJobs[key] = job;
        return Task.CompletedTask;
    }

    Task<CronJob?> ICronJobStore.FindAsync(string name, string @namespace)
    {
        string key = @namespace + '/' + name;
        return Task.FromResult(_cronJobs.TryGetValue(key, out var job) ? job : null);
    }

    Task ICronJobStore.DeleteAsync(string name, string @namespace)
    {
        string key = @namespace + '/' + name;
        _cronJobs.Remove(key);
        return Task.CompletedTask;
    }
}
