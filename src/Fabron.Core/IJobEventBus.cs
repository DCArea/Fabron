
using System.Threading.Tasks;
using Fabron.Models;

namespace Fabron
{
    public interface IJobEventBus
    {
        Task OnJobStateChanged(Job jobState);

        Task OnCronJobStateChanged(CronJob jobState);

        Task OnJobExecutionFailed(Job jobState, string reason);
    }
}
