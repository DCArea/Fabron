using Fabron.CloudEvents;
using Fabron.Models;
using Fabron.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Toolkit.Diagnostics;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Fabron.Schedulers;

public interface ICronScheduler : IGrainWithStringKey
{
    [ReadOnly]
    ValueTask<CronEvent?> GetState();

    [ReadOnly]
    Task<TickerStatus> GetTickerStatus();

    Task<CronEvent> Schedule(
        string template,
        CronEventSpec spec,
        Dictionary<string, string>? labels,
        Dictionary<string, string>? annotations,
        string? owner
    );

    Task Unregister();
}

public class CronEventScheduler : SchedulerGrain<CronEvent>, IGrainBase, ICronScheduler
{
    private readonly CronSchedulerOptions _options;

    public CronEventScheduler(
        IGrainContext context,
        IGrainRuntime runtime,
        ILogger<CronEventScheduler> logger,
        IOptions<CronSchedulerOptions> options,
        ISystemClock clock,
        ICronEventStore store,
        IEventDispatcher dispatcher) : base(context, runtime, logger, clock, options.Value, store, dispatcher) => _options = options.Value;

    async Task IGrainBase.OnActivateAsync(CancellationToken cancellationToken)
    {
        _key = this.GetPrimaryKeyString();
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

    public ValueTask<CronEvent?> GetState() => new(_state);

    public async Task<CronEvent> Schedule(
        string template,
        CronEventSpec spec,
        Dictionary<string, string>? labels,
        Dictionary<string, string>? annotations,
        string? owner)
    {
        var utcNow = _clock.UtcNow;
        _state = new CronEvent
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

        if (!_state.Spec.Suspend && (_state.Spec.NotBefore is null || _state.Spec.NotBefore.Value <= utcNow))
        {
            await Tick(default);
        }
        return _state;
    }

    internal override async Task Tick(DateTimeOffset expectedTickTime)
    {
        var now = _clock.UtcNow;
        TickerLog.Ticking(_logger, _key, now, expectedTickTime);

        var shouldDispatchForCurrentTick = expectedTickTime != default;
        if (shouldDispatchForCurrentTick && now.Subtract(expectedTickTime) > TimeSpan.FromMinutes(5))
        {
            TickerLog.UnexpectedTick(_logger, _key, expectedTickTime, "Missed");
            shouldDispatchForCurrentTick = false;
        }
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

        var cron = Cronos.CronExpression.Parse(_state.Spec.Schedule, _options.CronFormat);



        var from = shouldDispatchForCurrentTick ? expectedTickTime : now;
        var to = from.AddMinutes(2);
        if (to > _state.Spec.ExpirationTime)
        {
            to = _state.Spec.ExpirationTime.Value;
        }

        var schedules = cron.GetOccurrences(from, to, _options.TimeZone, fromInclusive: false, toInclusive: false);
        if (shouldDispatchForCurrentTick)
        {
            Dispatch(expectedTickTime);
        }
        foreach (var schedule in schedules)
        {
            Dispatch(schedule);
        }

        from = to;
        var nextTick = cron.GetNextOccurrence(from, _options.TimeZone, inclusive: true);
        if (!nextTick.HasValue || (_state.Spec.ExpirationTime.HasValue && nextTick.Value > _state.Spec.ExpirationTime.Value))
        {
            // no more next tick
            await StopTicker();
            return;
        }
        else
        {
            await TickAfter(nextTick.Value.Subtract(now));
        }
    }

    private void Dispatch(DateTimeOffset schedule)
    {
        Guard.IsNotNull(_state, nameof(_state));
        var now = _clock.UtcNow;
        var dueTime = schedule > now ? schedule.Subtract(now) : TimeSpan.Zero;
        var cloudEvent = _state.ToCloudEvent(schedule, _options.JsonSerializerOptions);
        _runtime.TimerRegistry.RegisterTimer(
            GrainContext,
            obj => DispatchNew((CloudEventEnvelop)obj),
            cloudEvent,
            dueTime,
            Timeout.InfiniteTimeSpan);
    }
}
