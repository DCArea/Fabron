using Fabron.CloudEvents;
using Fabron.Models;
using Fabron.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Toolkit.Diagnostics;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Fabron.Schedulers;

public interface IPeriodicScheduler : IGrainWithStringKey
{
    [ReadOnly]
    ValueTask<PeriodicEvent?> GetState();

    [ReadOnly]
    Task<TickerStatus> GetTickerStatus();

    Task<PeriodicEvent> Schedule(
        string template,
        PeriodicEventSpec spec,
        Dictionary<string, string>? labels,
        Dictionary<string, string>? annotations,
        string? owner
    );

    Task Unregister();
}

public class PeriodicEventScheduler : SchedulerGrain<PeriodicEvent>, IGrainBase, IPeriodicScheduler
{
    private readonly PeriodicSchedulerOptions _options;

    public PeriodicEventScheduler(
        IGrainContext context,
        IGrainRuntime runtime,
        ILogger<PeriodicEventScheduler> logger,
        IOptions<PeriodicSchedulerOptions> options,
        ISystemClock clock,
        IPeriodicEventStore store,
        IEventDispatcher dispatcher) : base(context, runtime, logger, clock, options.Value, store, dispatcher) => _options = options.Value;

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

    public ValueTask<PeriodicEvent?> GetState() => new(_state);

    public async Task<PeriodicEvent> Schedule(
        string template,
        PeriodicEventSpec spec,
        Dictionary<string, string>? labels,
        Dictionary<string, string>? annotations,
        string? owner)
    {
        Guard.IsGreaterThanOrEqualTo(spec.Period, TimeSpan.FromSeconds(5), nameof(spec.Period));

        var utcNow = _clock.UtcNow;
        _state = new PeriodicEvent
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

        if (!_state.Spec.Suspend && (_state.Spec.NotBefore is null || _state.Spec.NotBefore.Value <= utcNow))
        {
            await Tick(utcNow);
        }
        return _state;
    }

    internal override async Task Tick(DateTimeOffset expectedTickTime)
    {
        if (_state is null || _state.Metadata.DeletionTimestamp is not null)
        {
            TickerLog.UnexpectedTick(_logger, _key, expectedTickTime, "NotRegistered");
            await StopTicker();
            return;
        }

        if (_state.Spec.Suspend)
        {
            TickerLog.UnexpectedTick(_logger, _key, expectedTickTime, "Suspended");
            await StopTicker();
            return;
        }

        var now = _clock.UtcNow;
        if (now > _state.Spec.ExpirationTime)
        {
            TickerLog.UnexpectedTick(_logger, _key, expectedTickTime, "Expired");
            await StopTicker();
            return;
        }

        if (_state.Spec.NotBefore.HasValue && now < _state.Spec.NotBefore.Value)
        {
            TickerLog.UnexpectedTick(_logger, _key, expectedTickTime, "NotStarted");
            await TickAfter(_state.Spec.NotBefore.Value.Subtract(now));
            return;
        }

        var to = now.AddMinutes(1);
        if (_state.Spec.ExpirationTime.HasValue && to > _state.Spec.ExpirationTime)
        {
            to = _state.Spec.ExpirationTime.Value;
        }
        var nextTick = Dispatch(now, to);
        if (_state.Spec.ExpirationTime.HasValue && nextTick > _state.Spec.ExpirationTime.Value)
        {
            // no more next tick
            await StopTicker();
            return;
        }
        await TickAfter(nextTick.Subtract(now));
    }

    private DateTimeOffset Dispatch(DateTimeOffset now, DateTimeOffset to)
    {
        Guard.IsNotNull(_state, nameof(_state));
        var schedule = now;
        var dueTime = TimeSpan.Zero;
        while (schedule < to)
        {
            var cloudEvent = _state.ToCloudEvent(schedule, _options.JsonSerializerOptions);
            _runtime.TimerRegistry.RegisterTimer(
                GrainContext,
                obj => DispatchNew((CloudEventEnvelop)obj),
                cloudEvent,
                dueTime,
                Timeout.InfiniteTimeSpan);
            dueTime = dueTime.Add(_state.Spec.Period);
            schedule = now.Add(dueTime);
        }
        return schedule;
    }
}
