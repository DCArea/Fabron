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

    public Task ScheduleTimedEvent(
        string key,
        string? data,
        DateTimeOffset schedule,
        Dictionary<string, string>? extensions = null)
    {
        var grain = _client.GetGrain<ITimedScheduler>(key);
        var spec = new TimedEventSpec
        {
            Schedule = schedule,
        };
        return grain.Schedule(data, spec, null, extensions);
    }

    public Task CancelTimedEvent(string key)
    {
        var grain = _client.GetGrain<ITimedScheduler>(key);
        return grain.Unregister();
    }

    public async Task<TimedEvent?> GetTimedEvent(string key)
    {
        var grain = _client.GetGrain<ITimedScheduler>(key);
        var state = await grain.GetState();
        return state;
    }

    public Task<TickerStatus> GetTimedEventTickerStatus(string key)
    {
        return _client.GetGrain<ITimedScheduler>(key)
            .GetTickerStatus();
    }

    public async Task ScheduleCronEvent(
        string key,
        string? data,
        string schedule,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? expirationTime = null,
        bool suspend = false,
        Dictionary<string, string>? extensions = null)
    {
        var grain = _client.GetGrain<ICronScheduler>(key);
        var spec = new CronEventSpec
        {
            Schedule = schedule,
            NotBefore = notBefore,
            ExpirationTime = expirationTime,
            Suspend = suspend,
        };
        await grain.Schedule(data, spec, null, extensions);
    }

    public async Task<CronEvent?> GetCronEvent(string key)
    {
        var grain = _client.GetGrain<ICronScheduler>(key);
        var state = await grain.GetState();
        return state;
    }

    public Task<TickerStatus> GetCronEventTickerStatus(string key)
    {
        return _client.GetGrain<ICronScheduler>(key)
            .GetTickerStatus();
    }

    public async Task CancelCronEvent(string key)
    {
        var grain = _client.GetGrain<ICronScheduler>(key);
        await grain.Unregister();
    }

    public async Task SchedulePeriodicEvent(
        string key,
        string? data,
        TimeSpan period,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? expirationTime = null,
        bool suspend = false,
        Dictionary<string, string>? extensions = null)
    {
        var grain = _client.GetGrain<IPeriodicScheduler>(key);
        var spec = new PeriodicEventSpec
        {
            Period = period,
            NotBefore = notBefore,
            ExpirationTime = expirationTime,
            Suspend = suspend,
        };
        await grain.Schedule(data, spec, null, extensions);
    }

    public async Task<PeriodicEvent?> GetPeriodicEvent(string key)
    {
        var grain = _client.GetGrain<IPeriodicScheduler>(key);
        var state = await grain.GetState();
        return state;
    }

    public Task<TickerStatus> GetPeriodicEventTickerStatus(string key)
    {
        return _client.GetGrain<IPeriodicScheduler>(key)
            .GetTickerStatus();
    }

    public Task CancelPeriodicEvent(string key)
    {
        var grain = _client.GetGrain<IPeriodicScheduler>(key);
        return grain.Unregister();
    }

}
