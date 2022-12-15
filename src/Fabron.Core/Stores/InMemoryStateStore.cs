using Fabron.Models;

namespace Fabron.Stores;

public class InMemoryTimedEventStore : InMemoryStateStore<TimedEvent>, ITimedEventStore
{ }

public class InMemoryCronEventStore : InMemoryStateStore<CronEvent>, ICronEventStore
{ }

public class InMemoryPeriodicEventStore : InMemoryStateStore<PeriodicEvent>, IPeriodicEventStore
{ }

public abstract class InMemoryStateStore<TState> : IStateStore<TState>
    where TState : IScheduledEvent
{
    private StateEntry<TState>? _entry;

    public Task<StateEntry<TState>?> GetAsync(string key) => Task.FromResult(_entry);

    public Task RemoveAsync(string key, string? expectedETag)
    {
        _entry = null;
        return Task.CompletedTask;
    }

    public Task<string> SetAsync(TState state, string? expectedETag)
    {
        var newETag = Guid.NewGuid().ToString();
        _entry = new StateEntry<TState>(state, newETag);
        return Task.FromResult(newETag);
    }
}
