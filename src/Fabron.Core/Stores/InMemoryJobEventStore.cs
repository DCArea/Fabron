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

        public Task<long> GetConsumerOffset(string entityKey)
        {
            if (!_consumerOffsets.TryGetValue(entityKey, out long consumerOffset))
            {
                return Task.FromResult(-1L);
            }
            return Task.FromResult(consumerOffset);
        }
        public Task SaveConsumerOffset(string entityKey, long consumerOffset)
        {
            _consumerOffsets[entityKey] = consumerOffset;
            return Task.CompletedTask;
        }
        public Task ClearConsumerOffset(string entityKey)
        {
            _consumerOffsets.Remove(entityKey);
            return Task.CompletedTask;
        }

        public Task CommitEventLog(EventLog eventLog)
        {
            if (!_storage.TryGetValue(eventLog.EntityKey, out Dictionary<long, EventLog>? feed))
            {
                feed = new Dictionary<long, EventLog>();
                _storage.Add(eventLog.EntityKey, feed);
            }
            feed.Add(eventLog.Version, eventLog);
            return Task.CompletedTask;
        }

        public Task<List<EventLog>> GetEventLogs(string entityKey, long minVersion)
        {
            if (!_storage.TryGetValue(entityKey, out Dictionary<long, EventLog>? feed))
            {
                feed = new Dictionary<long, EventLog>();
                _storage.Add(entityKey, feed);
            }

            var eventLogs = feed.Values
                .OrderBy(log => log.Version)
                .Where(log => log.Version >= minVersion)
                .ToList();
            return Task.FromResult(eventLogs);
        }

        public Task ClearEventLogs(string entityKey, long maxVersion)
        {
            if (!_storage.TryGetValue(entityKey, out Dictionary<long, EventLog>? feed))
            {
                feed = new Dictionary<long, EventLog>();
                _storage.Add(entityKey, feed);
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
