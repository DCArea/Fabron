
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fabron.Core.CloudEvents;
using Fabron.Diagnostics;
using Fabron.Models;
using Fabron.Store;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Toolkit.Diagnostics;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Fabron.Schedulers;

public interface ITimedEventScheduler : IGrainWithStringKey
{
    [ReadOnly]
    Task<TickerStatus> GetTickerStatus();

    [ReadOnly]
    ValueTask<TimedEvent?> GetState();

    Task<TimedEvent> Schedule(
        TimedEventSpec spec,
        Dictionary<string, string>? labels,
        Dictionary<string, string>? annotations,
        string? owner
    );

    Task Unregister();
}

public partial class TimedEventScheduler : TickerGrain, IGrainBase, ITimedEventScheduler
{
    private readonly ILogger _logger;
    private readonly ISystemClock _clock;
    private readonly ITimedEventStore _store;
    private readonly SimpleSchedulerOptions _options;
    private readonly IEventDispatcher _dispatcher;

    public TimedEventScheduler(
        IGrainContext context,
        IGrainRuntime runtime,
        ILogger<TimedEventScheduler> logger,
        IOptions<SimpleSchedulerOptions> options,
        ISystemClock clock,
        ITimedEventStore store,
        IEventDispatcher mediator) : base(context, runtime, logger, options.Value.TickerInterval)
    {
        _logger = logger;
        _clock = clock;
        _store = store;
        _options = options.Value;
        _dispatcher = mediator;
    }

    private TimedEvent? _state;
    private string? _eTag;
    async Task IGrainBase.OnActivateAsync(CancellationToken cancellationToken)
    {
        _key = GrainContext.GrainReference.GetPrimaryKeyString();
        (_state, _eTag) = await _store.GetAsync(_key);
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
        Meters.TimedEventScheduledCount.Add(1);
        return _state;
    }

    internal override async Task Tick(DateTimeOffset? expectedTickTime)
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
    public static partial class Log
    {
        [LoggerMessage(
            EventId = 23000,
            Level = LogLevel.Warning,
            Message = "[{key}]: Schedule {schedule} is in the past, but still ticking now.")]
        public static partial void TickingForPast(ILogger logger, string key, DateTimeOffset schedule);
    }

    private async Task DispatchNew(CloudEventEnvelop cloudEvent)
    {
        Guard.IsNotNull(_state, nameof(_state));
        var utcNow = _clock.UtcNow;
        var sw = ValueStopwatch.StartNew();
        RecordTick(utcNow);
        Meters.RecordCloudEventDispatchTardiness(utcNow, cloudEvent.Time);
        try
        {
            await _dispatcher.DispatchAsync(_state.Metadata, cloudEvent);
            Meters.CloudEventDispatchCount.Add(1);
            Meters.CloudEventDispatchDuration.Record(sw.GetElapsedTime().TotalMilliseconds);
        }
        catch (Exception e)
        {
            TickerLog.ErrorOnTicking(_logger, _key, e);
            Meters.CloudEventDispatchFailedCount.Add(1);
            return;
        }
    }
}

