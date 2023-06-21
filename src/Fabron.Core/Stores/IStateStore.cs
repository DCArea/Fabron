using Fabron.Models;

namespace Fabron.Stores;

public record StateEntry<TState>(
    TState State,
    string? ETag
);

public interface IStateStore<TState>
    where TState : IDistributedTimer
{
    Task<string> SetAsync(TState state, string? expectedETag);
    Task<StateEntry<TState>?> GetAsync(string key);
    Task RemoveAsync(string key, string? expectedETag);
}

public interface IGenericTimerStore : IStateStore<GenericTimer>
{ }
public interface ICronTimerStore : IStateStore<CronTimer>
{ }
public interface IPeriodicTimerStore : IStateStore<PeriodicTimer>
{ }
