using Fabron.Models;
using Fabron.Schedulers;

namespace Fabron;

internal sealed class PeriodicTimerManager(IClusterClient client) : TimerManager<IPeriodicScheduler, PeriodicTimer, PeriodicTimerSpec>(client), IPeriodicTimerManager
{
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
