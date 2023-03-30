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
