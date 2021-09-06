using System.Threading.Tasks;

namespace Fabron.Models.Events
{
    public interface IJobEvent { };

    public record JobStateChanged(string JobId) : IJobEvent;
    public record CronJobStateChanged(string CronJobId) : IJobEvent;

    public record JobExecutionFailed(string JobId, string Reason) : IJobEvent;

    public interface IJobEventHandler<TJobEvent> where TJobEvent : IJobEvent
    {
        Task On(TJobEvent @event);
    }
}
