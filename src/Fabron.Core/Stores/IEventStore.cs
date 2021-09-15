using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron.Events;

namespace Fabron.Stores
{
    public interface IEventStore
    {
        Task<long> GetConsumerOffset(string entityKey);
        Task CommitEventLog(EventLog eventLog);
        Task ClearConsumerOffset(string entityKey);

        Task<List<EventLog>> GetEventLogs(string entityKey, long minVersion);
        Task SaveConsumerOffset(string entityKey, long consumerOffset);
        Task ClearEventLogs(string entityKey, long maxVersion);
    }

    public interface IJobEventStore: IEventStore
    {
    }

    public interface ICronJobEventStore: IEventStore
    {
    }
}
