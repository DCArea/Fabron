using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabron.Events;

namespace Fabron.Stores
{
    public abstract class InMemoryEventStore : IEventStore
    {
        private readonly Dictionary<string, EventLog> _storage = new();
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
            var key = eventLog.EntityKey + "_" + eventLog.Version;
            _storage.Add(key, eventLog);
            return Task.CompletedTask;
        }

        public Task<List<EventLog>> GetEventLogs(string entityKey, long minVersion)
        {
            var eventLogs = _storage.Values
                .Where(x => x.EntityKey == entityKey && x.Version >= minVersion)
                .OrderBy(log => log.Version)
                .ToList();
            return Task.FromResult(eventLogs);
        }

        public Task ClearEventLogs(string entityKey, long maxVersion)
        {
            foreach (var eventLog in _storage.Values.Where(k => k.EntityKey == entityKey && k.Version < maxVersion))
            {
                var key = eventLog.EntityKey + "_" + eventLog.Version;
                _storage.Remove(key);
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
