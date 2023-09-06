using Fabron.Models;
using Fabron.Schedulers;

namespace Fabron;

internal class TimerManager<TScheduler, TTimer, TTimerSpec>(IClusterClient client) : ITimerManager<TTimer>
    where TScheduler : ISchedulerGrain<TTimer, TTimerSpec>
    where TTimer : DistributedTimer<TTimerSpec>
    where TTimerSpec : ISchedulerSpec
{
    protected TScheduler GetScheduler(string key) => client.GetGrain<TScheduler>(key);

    public Task Start(string key)
        => GetScheduler(key).Start();

    public Task Stop(string key)
        => GetScheduler(key).Stop();

    public Task Tick(string key)
        => GetScheduler(key).Tick();

    public Task SetExt(
        string key,
        Dictionary<string, string?> extensions)
        => GetScheduler(key).SetExt(extensions);

    public Task Delete(string key)
        => GetScheduler(key).Delete();

    public ValueTask<TTimer?> Get(string key)
        => GetScheduler(key).GetState();
}
