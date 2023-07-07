using Fabron.Models;
using Fabron.Schedulers;

namespace Fabron;

internal class CronTimerManager : TimerManager<ICronScheduler, CronTimer, CronTimerSpec>, ICronTimerManager
{
    public CronTimerManager(IClusterClient client) : base(client)
    { }

    public Task Schedule(
        string key,
        string? data,
        string cron,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? notAfter = null,
        Dictionary<string, string>? extensions = null)
    {
        var spec = new CronTimerSpec
        (
            Schedule: cron,
            NotBefore: notBefore,
            NotAfter: notAfter
        );
        return GetScheduler(key).Schedule(data, spec, null, extensions);
    }
}
