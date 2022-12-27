using Fabron.Models;

namespace Fabron.Stores;

public record StateEntry<TState>(
    TState State,
    string? ETag
);

public interface IStateStore<TState>
    where TState : IScheduledEvent
{
    Task<string> SetAsync(TState state, string? expectedETag);
    Task<StateEntry<TState>?> GetAsync(string key);
    Task RemoveAsync(string key, string? expectedETag);
}

public interface ITimedEventStore : IStateStore<TimedEvent>
{ }
public interface ICronEventStore : IStateStore<CronEvent>
{ }
public interface IPeriodicEventStore : IStateStore<PeriodicEvent>
{ }
