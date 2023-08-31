using Fabron.Models;

namespace Fabron;

public sealed class FabronClient : IFabronClient
{
    public FabronClient(IClusterClient client)
    {
        Generic = new GenericTimerManager(client);
        Periodic = new PeriodicTimerManager(client);
        Cron = new CronTimerManager(client);
    }
    public IGenericTimerManager Generic { get; }
    public IPeriodicTimerManager Periodic { get; }
    public ICronTimerManager Cron { get; }

    public Task ScheduleGenericTimer(
        string key,
        string? data,
        DateTimeOffset schedule,
        Dictionary<string, string>? extensions = null)
        => Generic.Schedule(key, data, schedule, extensions);

    public Task SetExtForGenericTimer(
        string key,
        Dictionary<string, string?> extensions)
        => Generic.SetExt(key, extensions);

    public Task StartGenericTimer(string key)
        => Generic.Start(key);

    public Task StopGenericTimer(string key)
        => Generic.Stop(key);

    public Task DeleteGenericTimer(string key)
        => Generic.Delete(key);

    public Task<GenericTimer?> GetGenericTimer(string key)
        => Generic.Get(key).AsTask();

    public Task ScheduleCronTimer(
        string key,
        string? data,
        string schedule,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? notAfter = null,
        Dictionary<string, string>? extensions = null)
        => Cron.Schedule(key, data, schedule, notBefore, notAfter, extensions);

    public Task SetExtForCronTimer(
        string key,
        Dictionary<string, string?> extensions)
        => Cron.SetExt(key, extensions);

    public Task<CronTimer?> GetCronTimer(string key)
        => Cron.Get(key).AsTask();

    public Task StartCronTimer(string key)
        => Cron.Start(key);

    public Task StopCronTimer(string key)
        => Cron.Stop(key);

    public Task DeleteCronTimer(string key)
        => Cron.Delete(key);

    public Task SchedulePeriodicTimer(
        string key,
        string? data,
        TimeSpan period,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? notAfter = null,
        Dictionary<string, string>? extensions = null)
        => Periodic.Schedule(key, data, period, notBefore, notAfter, extensions);

    public Task SetExtForPeriodicTimer(
        string key,
        Dictionary<string, string?> extensions)
        => Periodic.SetExt(key, extensions);

    public Task<PeriodicTimer?> GetPeriodicTimer(string key)
        => Periodic.Get(key).AsTask();

    public Task StartPeriodicTimer(string key)
        => Periodic.Start(key);

    public Task StopPeriodicTimer(string key)
        => Periodic.Stop(key);

    public Task DeletePeriodicTimer(string key)
        => Periodic.Delete(key);

}
