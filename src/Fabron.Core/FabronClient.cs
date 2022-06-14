using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Fabron.Core.CloudEvents;
using Fabron.Models;
using Fabron.Schedulers;
using Microsoft.Extensions.Options;
using Orleans;

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

    public async Task ScheduleTimedEvent<T>(
        string key,
        DateTimeOffset schedule,
        CloudEventTemplate<T> template,
        Dictionary<string, string>? labels = null,
        Dictionary<string, string>? annotations = null)
    {
        var grain = _client.GetGrain<ITimedEventScheduler>(key);
        var spec = new TimedEventSpec
        {
            Schedule = schedule,
            Template = JsonSerializer.Serialize(template, _options.JsonSerializerOptions),
        };
        await grain.Schedule(spec, labels, annotations, null);
    }

    public async Task CancelTimedEvent(string key)
    {
        var grain = _client.GetGrain<ITimedEventScheduler>(key);
        await grain.Unregister();
    }

    public async Task<TimedEvent<TData>?> GetTimedEvent<TData>(string key)
    {
        var grain = _client.GetGrain<ITimedEventScheduler>(key);
        var state = await grain.GetState();
        if (state is null) return null;
        return new TimedEvent<TData>(
            state.Metadata,
            new(
                state.Spec.Schedule,
                JsonSerializer.Deserialize<CloudEventTemplate<TData>>(state.Spec.Template, _options.JsonSerializerOptions)!
            )
        );
    }

    public Task<TickerStatus> GetTimedEventTickerStatus(string key)
    {
        return _client.GetGrain<ITimedEventScheduler>(key)
            .GetTickerStatus();
    }

    public async Task ScheduleCronEvent<T>(
        string key,
        string schedule,
        CloudEventTemplate<T> template,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? expirationTime = null,
        bool suspend = false,
        Dictionary<string, string>? labels = null,
        Dictionary<string, string>? annotations = null)
    {
        var grain = _client.GetGrain<ICronEventScheduler>(key);
        var spec = new CronEventSpec
        {
            Template = JsonSerializer.Serialize(template, _options.JsonSerializerOptions),
            Schedule = schedule,
            NotBefore = notBefore,
            ExpirationTime = expirationTime,
            Suspend = suspend,
        };
        await grain.Schedule(spec, labels, annotations, null);
    }

    public async Task<CronEvent<TData>?> GetCronEvent<TData>(string key)
    {
        var grain = _client.GetGrain<ICronEventScheduler>(key);
        var state = await grain.GetState();
        if (state is null) return null;
        return new CronEvent<TData>(
            state.Metadata,
            new(
                state.Spec.Schedule,
                JsonSerializer.Deserialize<CloudEventTemplate<TData>>(state.Spec.Template, _options.JsonSerializerOptions)!,
                state.Spec.NotBefore,
                state.Spec.ExpirationTime,
                state.Spec.Suspend
            )
        );
    }

    public Task<TickerStatus> GetCronEventTickerStatus(string key)
    {
        return _client.GetGrain<ICronEventScheduler>(key)
            .GetTickerStatus();
    }

    public async Task CancelCronEvent(string key)
    {
        var grain = _client.GetGrain<ICronEventScheduler>(key);
        await grain.Unregister();
    }

    public async Task SchedulePeriodicEvent<T>(
        string key,
        CloudEventTemplate<T> template,
        TimeSpan period,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? expirationTime = null,
        bool suspend = false,
        Dictionary<string, string>? labels = null,
        Dictionary<string, string>? annotations = null)
    {
        var grain = _client.GetGrain<IPeriodicEventScheduler>(key);
        var spec = new PeriodicEventSpec
        {
            Template = JsonSerializer.Serialize(template, _options.JsonSerializerOptions),
            Period = period,
            NotBefore = notBefore,
            ExpirationTime = expirationTime,
            Suspend = suspend,
        };
        await grain.Schedule(spec, labels, annotations, null);
    }

    public async Task<PeriodicEvent<TData>?> GetPeriodicEvent<TData>(string key)
    {
        var grain = _client.GetGrain<IPeriodicEventScheduler>(key);
        var state = await grain.GetState();
        if (state is null) return null;
        return new PeriodicEvent<TData>(
            state.Metadata,
            new(
                JsonSerializer.Deserialize<CloudEventTemplate<TData>>(state.Spec.Template, _options.JsonSerializerOptions)!,
                state.Spec.Period,
                state.Spec.NotBefore,
                state.Spec.ExpirationTime,
                state.Spec.Suspend
            )
        );
    }

    public Task<TickerStatus> GetPeriodicEventTickerStatus(string key)
    {
        return _client.GetGrain<IPeriodicEventScheduler>(key)
            .GetTickerStatus();
    }

    public Task CancelPeriodicEvent(string key)
    {
        var grain = _client.GetGrain<IPeriodicEventScheduler>(key);
        return grain.Unregister();
    }

}
