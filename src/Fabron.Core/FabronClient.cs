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
        var grain = _client.GetGrain<ITimedScheduler>(key);
        var spec = new GenericTimerSpec
        {
            Schedule = schedule,
        };
        return grain.Schedule(data, spec, null, extensions);
    }

    public Task CancelGenericTimer(string key)
    {
        var grain = _client.GetGrain<ITimedScheduler>(key);
        return grain.Unregister();
    }

    public async Task<GenericTimer?> GetGenericTimer(string key)
    {
        var grain = _client.GetGrain<ITimedScheduler>(key);
        var state = await grain.GetState();
        return state;
    }

    public Task<TickerStatus> GetGenericTimerTickerStatus(string key)
    {
        return _client.GetGrain<ITimedScheduler>(key)
            .GetTickerStatus();
    }

    public async Task ScheduleCronTimer(
        string key,
        string? data,
        string schedule,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? expirationTime = null,
        bool suspend = false,
        Dictionary<string, string>? extensions = null)
    {
        var grain = _client.GetGrain<ICronScheduler>(key);
        var spec = new CronTimerSpec
        {
            Schedule = schedule,
            NotBefore = notBefore,
            ExpirationTime = expirationTime,
            Suspend = suspend,
        };
        await grain.Schedule(data, spec, null, extensions);
    }

    public async Task<CronTimer?> GetCronTimer(string key)
    {
        var grain = _client.GetGrain<ICronScheduler>(key);
        var state = await grain.GetState();
        return state;
    }

    public Task<TickerStatus> GetCronTimerTickerStatus(string key)
    {
        return _client.GetGrain<ICronScheduler>(key)
            .GetTickerStatus();
    }

    public async Task CancelCronTimer(string key)
    {
        var grain = _client.GetGrain<ICronScheduler>(key);
        await grain.Unregister();
    }

    public async Task SchedulePeriodicTimer(
        string key,
        string? data,
        TimeSpan period,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? expirationTime = null,
        bool suspend = false,
        Dictionary<string, string>? extensions = null)
    {
        var grain = _client.GetGrain<IPeriodicScheduler>(key);
        var spec = new PeriodicTimerSpec
        {
            Period = period,
            NotBefore = notBefore,
            ExpirationTime = expirationTime,
            Suspend = suspend,
        };
        await grain.Schedule(data, spec, null, extensions);
    }

    public async Task<Models.PeriodicTimer?> GetPeriodicTimer(string key)
    {
        var grain = _client.GetGrain<IPeriodicScheduler>(key);
        var state = await grain.GetState();
        return state;
    }

    public Task<TickerStatus> GetPeriodicTimerTickerStatus(string key)
    {
        return _client.GetGrain<IPeriodicScheduler>(key)
            .GetTickerStatus();
    }

    public Task CancelPeriodicTimer(string key)
    {
        var grain = _client.GetGrain<IPeriodicScheduler>(key);
        return grain.Unregister();
    }

}
