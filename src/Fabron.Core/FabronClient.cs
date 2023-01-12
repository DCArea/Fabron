using System.Text.Json;
using Fabron.Models;
using Fabron.Schedulers;
using Microsoft.Extensions.Options;

namespace Fabron;

public class FabronClient : IFabronClient
{
    private readonly IClusterClient _client;
    private readonly FabronClientOptions _options;

    public FabronClient(
        IClusterClient client,
        IOptions<FabronClientOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public Task ScheduleTimedEvent<TData>(
        string key,
        DateTimeOffset schedule,
        TData data,
        Dictionary<string, string>? extensions = null)
    {
        var grain = _client.GetGrain<ITimedScheduler>(key);
        var dataJSON = JsonSerializer.Serialize(data, _options.JsonSerializerOptions);
        var spec = new TimedEventSpec
        {
            Schedule = schedule,
        };
        return grain.Schedule(dataJSON, spec, null, extensions);
    }

    public Task CancelTimedEvent(string key)
    {
        var grain = _client.GetGrain<ITimedScheduler>(key);
        return grain.Unregister();
    }

    public async Task<TimedEvent<TData>?> GetTimedEvent<TData>(string key)
    {
        var grain = _client.GetGrain<ITimedScheduler>(key);
        var state = await grain.GetState();
        return state is null
            ? null
            : new TimedEvent<TData>(
            state.Metadata,
            JsonSerializer.Deserialize<TData>(state.Data, _options.JsonSerializerOptions)!,
            new()
            {
                Schedule = state.Spec.Schedule
            }
        );
    }

    public Task<TickerStatus> GetTimedEventTickerStatus(string key)
    {
        return _client.GetGrain<ITimedScheduler>(key)
            .GetTickerStatus();
    }

    public async Task ScheduleCronEvent<TData>(
        string key,
        string schedule,
        TData data,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? expirationTime = null,
        bool suspend = false,
        Dictionary<string, string>? extensions = null)
    {
        var grain = _client.GetGrain<ICronScheduler>(key);
        var dataJSON = JsonSerializer.Serialize(data, _options.JsonSerializerOptions);
        var spec = new CronEventSpec
        {
            Schedule = schedule,
            NotBefore = notBefore,
            ExpirationTime = expirationTime,
            Suspend = suspend,
        };
        await grain.Schedule(dataJSON, spec, null, extensions);
    }

    public async Task<CronEvent<TData>?> GetCronEvent<TData>(string key)
    {
        var grain = _client.GetGrain<ICronScheduler>(key);
        var state = await grain.GetState();
        return state is null
            ? null
            : new CronEvent<TData>(
            state.Metadata,
            JsonSerializer.Deserialize<TData>(state.Data, _options.JsonSerializerOptions)!,
            state.Spec
        );
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

    public async Task SchedulePeriodicEvent<TData>(
        string key,
        TData data,
        TimeSpan period,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? expirationTime = null,
        bool suspend = false,
        Dictionary<string, string>? extensions = null)
    {
        var grain = _client.GetGrain<IPeriodicScheduler>(key);
        var templateJSON = JsonSerializer.Serialize(data, _options.JsonSerializerOptions);
        var spec = new PeriodicEventSpec
        {
            Period = period,
            NotBefore = notBefore,
            ExpirationTime = expirationTime,
            Suspend = suspend,
        };
        await grain.Schedule(templateJSON, spec, null, extensions);
    }

    public async Task<PeriodicEvent<TData>?> GetPeriodicEvent<TData>(string key)
    {
        var grain = _client.GetGrain<IPeriodicScheduler>(key);
        var state = await grain.GetState();
        return state is null
            ? null
            : new PeriodicEvent<TData>(
            state.Metadata,
            JsonSerializer.Deserialize<TData>(state.Data, _options.JsonSerializerOptions)!,
            new()
            {
                Period = state.Spec.Period,
                NotBefore = state.Spec.NotBefore,
                ExpirationTime = state.Spec.ExpirationTime,
                Suspend = state.Spec.Suspend,
            }
        );
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
