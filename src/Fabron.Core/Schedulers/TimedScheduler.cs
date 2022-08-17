
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fabron.CloudEvents;
using Fabron.Diagnostics;
using Fabron.Models;
using Fabron.Store;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Fabron.Schedulers;

public interface ITimedScheduler : IGrainWithStringKey
{
    [ReadOnly]
    Task<TickerStatus> GetTickerStatus();

    [ReadOnly]
    ValueTask<TimedEvent?> GetState();

    Task<TimedEvent> Schedule(
        string template,
        TimedEventSpec spec,
        Dictionary<string, string>? labels,
        Dictionary<string, string>? annotations,
        string? owner
    );

    Task Unregister();
}

public partial class TimedEventScheduler : SchedulerGrain<TimedEvent>, IGrainBase, ITimedScheduler
{
    private readonly SimpleSchedulerOptions _options;

    public TimedEventScheduler(
        IGrainContext context,
        IGrainRuntime runtime,
        ILogger<TimedEventScheduler> logger,
        IOptions<SimpleSchedulerOptions> options,
        ISystemClock clock,
        ITimedEventStore store,
        IEventDispatcher dispatcher) : base(context, runtime, logger, clock, options.Value, store, dispatcher)
    {
        _options = options.Value;
    }

    async Task IGrainBase.OnActivateAsync(CancellationToken cancellationToken)
    {
        _key = GrainContext.GrainReference.GetPrimaryKeyString();
        var entry = await _store.GetAsync(_key);
        _state = entry?.State;
        _eTag = entry?.ETag;
    }

    public async Task Unregister()
    {
        if (_state is not null)
        {
            await _store.RemoveAsync(_state.Metadata.Key, _eTag);
            await StopTicker();
        }
    }

    public ValueTask<TimedEvent?> GetState() => ValueTask.FromResult(_state);

    public async Task<TimedEvent> Schedule(
        string template,
        TimedEventSpec spec,
        Dictionary<string, string>? labels,
        Dictionary<string, string>? annotations,
        string? owner)
    {
        var utcNow = _clock.UtcNow;
        var schedule_ = spec.Schedule;

        _state = new TimedEvent
        {
            Metadata = new()
            {
                Key = _key,
                CreationTimestamp = utcNow,
                Labels = labels,
                Annotations = annotations,
                Owner = owner
            },
            Template = template,
            Spec = spec
        };
        _eTag = await _store.SetAsync(_state, _eTag);

        utcNow = _clock.UtcNow;
        if (schedule_ <= utcNow)
        {
            Log.TickingForPast(_logger, _key, schedule_);
            await Tick(utcNow);
        }
        else
        {
            await TickAfter(schedule_ - utcNow);
        }
        Telemetry.CloudEventScheduled.Add(1);
        return _state;
    }

    internal override async Task Tick(DateTimeOffset expectedTickTime)
    {
        if (_state is null || _state.Metadata.DeletionTimestamp is not null)
        {
            TickerLog.UnexpectedTick(_logger, _key, expectedTickTime, "NotExists");
            await StopTicker();
            return;
        }

        var envelop = _state.ToCloudEvent(_state.Spec.Schedule, _options.JsonSerializerOptions);
        await DispatchNew(envelop);
        await StopTicker();
    }

    internal static partial class Log
    {
        [LoggerMessage(
            EventId = 23000,
            Level = LogLevel.Warning,
            Message = "[{key}]: Schedule {schedule} is in the past, but still ticking now.")]
        public static partial void TickingForPast(ILogger logger, string key, DateTimeOffset schedule);
    }
}

