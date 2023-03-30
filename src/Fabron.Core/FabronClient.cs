using System.Text.Json;
using Fabron.Models;
using Fabron.Schedulers;
using Microsoft.Extensions.Options;

namespace Fabron;

public class FabronClient : IFabronClient
{
    private readonly IClusterClient _client;

    public FabronClient(IClusterClient client)
    {
        _client = client;
    }

    public Task ScheduleGenericTimer(
        string key,
        string? data,
        DateTimeOffset schedule,
        Dictionary<string, string>? extensions = null)
    {
        var grain = _client.GetGrain<IGenericScheduler>(key);
        var spec = new GenericTimerSpec
        {
            Schedule = schedule,
        };
        return grain.Schedule(data, spec, null, extensions);
    }

    public Task StartGenericTimer(string key)
        => _client.GetGrain<IGenericScheduler>(key).Start();

    public Task StopGenericTimer(string key)
        => _client.GetGrain<IGenericScheduler>(key).Stop();

    public Task DeleteGenericTimer(string key)
        => _client.GetGrain<IGenericScheduler>(key).Stop();

    public async Task<GenericTimer?> GetGenericTimer(string key)
        => await _client.GetGrain<IGenericScheduler>(key).GetState();

    public Task<TickerStatus> GetGenericTimerTickerStatus(string key)
        => _client.GetGrain<IGenericScheduler>(key).GetTickerStatus();

    public async Task ScheduleCronTimer(
        string key,
        string? data,
        string schedule,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? expirationTime = null,
        Dictionary<string, string>? extensions = null)
    {
        var grain = _client.GetGrain<ICronScheduler>(key);
        var spec = new CronTimerSpec
        {
            Schedule = schedule,
            NotBefore = notBefore,
            ExpirationTime = expirationTime,
        };
        await grain.Schedule(data, spec, null, extensions);
    }

    public async Task<CronTimer?> GetCronTimer(string key)
        => await _client.GetGrain<ICronScheduler>(key).GetState();

    public Task<TickerStatus> GetCronTimerTickerStatus(string key)
        => _client.GetGrain<ICronScheduler>(key).GetTickerStatus();

    public Task StartCronTimer(string key)
        => _client.GetGrain<ICronScheduler>(key).Start();

    public Task StopCronTimer(string key)
        => _client.GetGrain<ICronScheduler>(key).Stop();

    public Task DeleteCronTimer(string key)
        => _client.GetGrain<ICronScheduler>(key).Delete();

    public async Task SchedulePeriodicTimer(
        string key,
        string? data,
        TimeSpan period,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? expirationTime = null,
        Dictionary<string, string>? extensions = null)
    {
        var grain = _client.GetGrain<IPeriodicScheduler>(key);
        var spec = new PeriodicTimerSpec
        {
            Period = period,
            NotBefore = notBefore,
            ExpirationTime = expirationTime,
        };
        await grain.Schedule(data, spec, null, extensions);
    }

    public async Task<Models.PeriodicTimer?> GetPeriodicTimer(string key)
        => await _client.GetGrain<IPeriodicScheduler>(key).GetState();

    public Task<TickerStatus> GetPeriodicTimerTickerStatus(string key)
        => _client.GetGrain<IPeriodicScheduler>(key).GetTickerStatus();

    public Task StartPeriodicTimer(string key)
        => _client.GetGrain<IPeriodicScheduler>(key).Start();

    public Task StopPeriodicTimer(string key)
        => _client.GetGrain<IPeriodicScheduler>(key).Stop();

    public Task DeletePeriodicTimer(string key)
        => _client.GetGrain<IPeriodicScheduler>(key).Delete();

}
