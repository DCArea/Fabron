using System.Text.Json;
using Fabron.CloudEvents;
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

    public Task ScheduleTimedEvent<T>(
        string key,
        DateTimeOffset schedule,
        CloudEventTemplate<T> template,
        Dictionary<string, string>? labels = null,
        Dictionary<string, string>? annotations = null)
    {
        var grain = _client.GetGrain<ITimedScheduler>(key);
        var templateJSON = JsonSerializer.Serialize(template, _options.JsonSerializerOptions);
        var spec = new TimedEventSpec
        {
            Schedule = schedule,
        };
        return grain.Schedule(templateJSON, spec, labels, annotations, null);
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
            JsonSerializer.Deserialize<CloudEventTemplate<TData>>(state.Template, _options.JsonSerializerOptions)!,
            new(
                state.Spec.Schedule
            )
        );
    }

    public Task<TickerStatus> GetTimedEventTickerStatus(string key)
    {
        return _client.GetGrain<ITimedScheduler>(key)
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
        var grain = _client.GetGrain<ICronScheduler>(key);
        var templateJSON = JsonSerializer.Serialize(template, _options.JsonSerializerOptions);
        var spec = new CronEventSpec
        {
            Schedule = schedule,
            NotBefore = notBefore,
            ExpirationTime = expirationTime,
            Suspend = suspend,
        };
        await grain.Schedule(templateJSON, spec, labels, annotations, null);
    }

    public async Task<CronEvent<TData>?> GetCronEvent<TData>(string key)
    {
        var grain = _client.GetGrain<ICronScheduler>(key);
        var state = await grain.GetState();
        return state is null
            ? null
            : new CronEvent<TData>(
            state.Metadata,
            JsonSerializer.Deserialize<CloudEventTemplate<TData>>(state.Template, _options.JsonSerializerOptions)!,
            new(
                state.Spec.Schedule,
                state.Spec.NotBefore,
                state.Spec.ExpirationTime,
                state.Spec.Suspend
            )
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
        var grain = _client.GetGrain<IPeriodicScheduler>(key);
        var templateJSON = JsonSerializer.Serialize(template, _options.JsonSerializerOptions);
        var spec = new PeriodicEventSpec
        {
            Period = period,
            NotBefore = notBefore,
            ExpirationTime = expirationTime,
            Suspend = suspend,
        };
        await grain.Schedule(templateJSON, spec, labels, annotations, null);
    }

    public async Task<PeriodicEvent<TData>?> GetPeriodicEvent<TData>(string key)
    {
        var grain = _client.GetGrain<IPeriodicScheduler>(key);
        var state = await grain.GetState();
        return state is null
            ? null
            : new PeriodicEvent<TData>(
            state.Metadata,
            JsonSerializer.Deserialize<CloudEventTemplate<TData>>(state.Template, _options.JsonSerializerOptions)!,
            new(
                state.Spec.Period,
                state.Spec.NotBefore,
                state.Spec.ExpirationTime,
                state.Spec.Suspend
            )
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
