using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabron.Events;

namespace Fabron.Stores
{
    public abstract class InMemoryEventStore : IEventStore
    {
        private readonly Dictionary<string, Dictionary<long, EventLog>> _storage = new();
        private readonly Dictionary<string, long> _consumerOffsets = new();

        public Task<long> GetConsumerOffset(string entityId)
        {
            if (!_consumerOffsets.TryGetValue(entityId, out long consumerOffset))
            {
                return Task.FromResult(-1L);
            }
            return Task.FromResult(consumerOffset);
        }
        public Task SaveConsumerOffset(string entityId, long consumerOffset)
        {
            _consumerOffsets[entityId] = consumerOffset;
            return Task.CompletedTask;
        }
        public Task ClearConsumerOffset(string entityId)
        {
            _consumerOffsets.Remove(entityId);
            return Task.CompletedTask;
        }

        public Task CommitEventLog(EventLog eventLog)
        {
            if (!_storage.TryGetValue(eventLog.EntityId, out Dictionary<long, EventLog>? feed))
            {
                feed = new Dictionary<long, EventLog>();
                _storage.Add(eventLog.EntityId, feed);
            }
            feed.Add(eventLog.Version, eventLog);
            return Task.CompletedTask;
        }

        public Task<List<EventLog>> GetEventLogs(string entityId, long minVersion)
        {
            if (!_storage.TryGetValue(entityId, out Dictionary<long, EventLog>? feed))
            {
                feed = new Dictionary<long, EventLog>();
                _storage.Add(entityId, feed);
            }

            var eventLogs = feed.Values
                .OrderBy(log => log.Version)
                .ToList();
            return Task.FromResult(eventLogs);
        }

        public Task ClearEventLogs(string entityId, long maxVersion)
        {
            if (!_storage.TryGetValue(entityId, out Dictionary<long, EventLog>? feed))
            {
                feed = new Dictionary<long, EventLog>();
                _storage.Add(entityId, feed);
            }

            foreach (long version in feed.Keys.Where(k => k < maxVersion))
            {
                feed.Remove(version);
            }

            return Task.CompletedTask;
        }

    }

    public class InMemoryJobEventStore : InMemoryEventStore, IJobEventStore
    {
    }

    public class InMemoryCronJobEventStore : InMemoryEventStore, ICronJobEventStore
    {
    }
}
