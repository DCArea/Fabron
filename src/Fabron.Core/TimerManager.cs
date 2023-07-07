using Fabron.Models;
using Fabron.Schedulers;

namespace Fabron;

internal class TimerManager<TScheduler, TTimer, TTimerSpec> : ITimerManager<TTimer>
    where TScheduler : ISchedulerGrain<TTimer, TTimerSpec>
    where TTimer : DistributedTimer<TTimerSpec>
    where TTimerSpec : ISchedulerSpec
{
    public TimerManager(IClusterClient client)
    {
        _client = client;
    }
    private readonly IClusterClient _client;

    protected TScheduler GetScheduler(string key) => _client.GetGrain<TScheduler>(key);

    public Task Start(string key)
        => GetScheduler(key).Start();

    public Task Stop(string key)
        => GetScheduler(key).Stop();

    public Task SetExt(
        string key,
        Dictionary<string, string?> extensions)
        => GetScheduler(key).SetExt(extensions);

    public Task Delete(string key)
        => GetScheduler(key).Delete();

    public ValueTask<TTimer?> Get(string key)
        => GetScheduler(key).GetState();
}
