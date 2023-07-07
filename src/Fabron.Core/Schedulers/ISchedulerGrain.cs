using Fabron.Models;
using Orleans.Concurrency;

namespace Fabron.Schedulers;

internal interface ISchedulerGrain<TTimer, TTimerSpec> : IGrainWithStringKey
    where TTimer : DistributedTimer<TTimerSpec>
    where TTimerSpec : ISchedulerSpec
{
    [ReadOnly]
    ValueTask<TTimer?> GetState();

    Task<TTimer> Schedule(
        string? data,
        TTimerSpec spec,
        string? owner,
        Dictionary<string, string>? extensions
    );

    Task SetExt(Dictionary<string, string?> input);

    Task Start();
    Task Stop();
    Task Delete();
}

