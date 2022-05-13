using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron.Models;

namespace Fabron.Store;

public interface IStateStore<TState>
{
    Task<string> SetAsync(TState state, string? expectedETag);
    Task<(TState? state, string? eTag)> GetAsync(string name, string @namespace);
    Task RemoveAsync(string name, string @namespace, string? expectedETag);
}

public interface IJobStore : IStateStore<Job>
{ }

public interface ICronJobStore : IStateStore<CronJob>
{ }

public abstract class InMemoryStateStore<TState> : IStateStore<TState>
{
    private readonly Dictionary<string, (TState state, string eTag)> _dict = new();

    public Task<(TState? state, string? eTag)> GetAsync(string name, string @namespace)
    {
        string key = @namespace + '/' + name;
        return Task.FromResult(_dict.TryGetValue(key, out var state) ? state : (default, default));
    }

    public Task RemoveAsync(string name, string @namespace, string? expectedETag)
    {
        string key = @namespace + '/' + name;
        _dict.Remove(key);
        return Task.CompletedTask;
    }

    public Task<string> SetAsync(TState state, string? expectedETag)
    {
        string key = GetStateKey(state);
        string newETag = Guid.NewGuid().ToString();
        _dict[key] = (state, newETag);
        return Task.FromResult(newETag);
    }

    protected abstract string GetStateKey(TState state);
}

public class InMemoryJobStore : InMemoryStateStore<Job>, IJobStore
{
    protected override string GetStateKey(Job state) => state.Metadata.Namespace + '/' + state.Metadata.Name;
}

public class InMemoryCronJobStore : InMemoryStateStore<CronJob>, ICronJobStore
{
    protected override string GetStateKey(CronJob state) => state.Metadata.Namespace + '/' + state.Metadata.Name;
}
