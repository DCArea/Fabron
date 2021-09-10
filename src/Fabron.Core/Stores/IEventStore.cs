using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron.Events;

namespace Fabron.Stores
{
    public interface IEventStore
    {
        Task<long> GetConsumerOffset(string entityId);
        Task CommitEventLog(EventLog eventLog);
        Task ClearConsumerOffset(string entityId);

        Task<List<EventLog>> GetEventLogs(string entityId, long minVersion);
        Task SaveConsumerOffset(string entityId, long consumerOffset);
        Task ClearEventLogs(string entityId, long maxVersion);
    }

    public interface IJobEventStore: IEventStore
    {
    }

    public interface ICronJobEventStore: IEventStore
    {
    }
}
