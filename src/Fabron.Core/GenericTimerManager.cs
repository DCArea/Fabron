using Fabron.Models;
using Fabron.Schedulers;

namespace Fabron;

internal sealed class GenericTimerManager(IClusterClient client) : TimerManager<IGenericScheduler, GenericTimer, GenericTimerSpec>(client), IGenericTimerManager
{
    public Task Schedule(
        string key,
        string? data,
        DateTimeOffset schedule,
        Dictionary<string, string>? extensions = null)
    {
        var spec = new GenericTimerSpec
        (
            Schedule: schedule
        );
        return GetScheduler(key).Schedule(data, spec, null, extensions);
    }
}
