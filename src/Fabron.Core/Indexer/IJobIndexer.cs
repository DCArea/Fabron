
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron.Models;

namespace Fabron
{
    public interface IJobIndexer
    {
        Task DeleteCronJob(string cronJobId);
        Task DeleteJob(string jobId);
        Task Index(Job job);
        Task Index(IEnumerable<Job> jobs);
        Task Index(CronJob job);
        Task Index(IEnumerable<CronJob> job);
    }

}
