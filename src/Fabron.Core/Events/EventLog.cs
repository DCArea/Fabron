using System;
using System.Text.Json;
using Microsoft.Toolkit.Diagnostics;

namespace Fabron.Events
{
    public record EventLog(
        string EntityId,
        long Version,
        DateTime Timestamp,
        string Type,
        string Data)
    {
        private object? _cache;
        public TEvent GetPayload<TEvent>() where TEvent : class
        {
            TEvent? @event = _cache is not null
                ? (TEvent)_cache
                : JsonSerializer.Deserialize<TEvent>(Data);
            _cache = @event;
            Guard.IsNotNull(@event, nameof(@event));
            return @event;
        }

        private EventLog(string entityId, long version, DateTime timestamp, string type, string data, object cache)
            : this(entityId, version, timestamp, type, data)
            => _cache = cache;

        public static EventLog Create<TEvent>(string entityId, long version, DateTime timestamp, string type, TEvent @event) where TEvent : class
            => new(
                entityId,
                version,
                timestamp,
                type,
                JsonSerializer.Serialize(@event),
                @event);
    };
}
