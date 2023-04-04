using Fabron.Models;

namespace Fabron.Stores;

public class InMemoryGenericTimerStore : InMemoryStateStore<GenericTimer>, IGenericTimerStore
{ }

public class InMemoryCronTimerStore : InMemoryStateStore<CronTimer>, ICronTimerStore
{ }

public class InMemoryPeriodicTimerStore : InMemoryStateStore<Models.PeriodicTimer>, IPeriodicTimerStore
{ }

public abstract class InMemoryStateStore<TState> : IStateStore<TState>
    where TState : IDistributedTimer
{
    private readonly Dictionary<string, StateEntry<TState>> _states = new();

    public Task<StateEntry<TState>?> GetAsync(string key)
    {
        _states.TryGetValue(key, out var entry);
        return Task.FromResult(entry);
    }

    public Task RemoveAsync(string key, string? expectedETag)
    {
        _states.Remove(key);
        return Task.CompletedTask;
    }

    public Task<string> SetAsync(TState state, string? expectedETag)
    {
        var newETag = Guid.NewGuid().ToString();
        var entry = new StateEntry<TState>(state, newETag);
        _states[state.Metadata.Key] = entry;
        return Task.FromResult(newETag);
    }
}
