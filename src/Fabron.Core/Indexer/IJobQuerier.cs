
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron.Models;

namespace Fabron
{
    public interface IJobQuerier
    {
        Task<Models.Job?> GetJobByKey(string key);
        Task<List<Job>> GetJobByLabel(string labelName, string labelValue);
        Task<List<Job>> GetJobByLabels(IEnumerable<(string, string)> labels);
        Task<Models.CronJob?> GetCronJobByKey(string key);
        Task<List<CronJob>> GetCronJobByLabel(string labelName, string labelValue);
        Task<List<CronJob>> GetCronJobByLabels(IEnumerable<(string, string)> labels);
    }

}
