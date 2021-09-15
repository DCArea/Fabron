
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron.Models;

namespace Fabron
{
    public interface IJobIndexer
    {
        Task DeleteCronJob(string key);
        Task DeleteJob(string key);
        Task Index(Job job);
        Task Index(IEnumerable<Job> jobs);
        Task Index(CronJob job);
        Task Index(IEnumerable<CronJob> job);
    }

}
