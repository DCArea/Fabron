using Fabron.Models;
using Fabron.Schedulers;

namespace Fabron;

internal sealed class PeriodicTimerManager : TimerManager<IPeriodicScheduler, PeriodicTimer, PeriodicTimerSpec>, IPeriodicTimerManager
{
    public PeriodicTimerManager(IClusterClient client) : base(client)
    { }

    public Task Schedule(
        string key,
        string? data,
        TimeSpan period,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? notAfter = null,
        Dictionary<string, string>? extensions = null)
    {
        var spec = new PeriodicTimerSpec
        (
            Period: period,
            NotBefore: notBefore,
            NotAfter: notAfter
        );
        return GetScheduler(key).Schedule(data, spec, null, extensions);
    }
}
