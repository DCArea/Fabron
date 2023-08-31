using Fabron.Models;
using Fabron.Schedulers;

namespace Fabron;

internal sealed class GenericTimerManager : TimerManager<IGenericScheduler, GenericTimer, GenericTimerSpec>, IGenericTimerManager
{
    public GenericTimerManager(IClusterClient client) : base(client)
    { }

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
