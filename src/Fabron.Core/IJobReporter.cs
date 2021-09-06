
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron.Models;

namespace Fabron
{
    public interface IJobReporter
    {
        Task Report(Job job);
        Task Report(IEnumerable<Job> jobs);
        Task Report(CronJob job);
        Task Report(IEnumerable<CronJob> job);
    }

    public class NoopJobReporter : IJobReporter
    {
        public Task Report(Job job) => Task.CompletedTask;
        public Task Report(CronJob job) => Task.CompletedTask;
        public Task Report(IEnumerable<Job> job) => Task.CompletedTask;
        public Task Report(IEnumerable<CronJob> job) => Task.CompletedTask;
    }
}
