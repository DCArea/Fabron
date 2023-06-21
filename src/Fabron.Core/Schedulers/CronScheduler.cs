using CommunityToolkit.Diagnostics;
using Fabron.Dispatching;
using Fabron.Models;
using Fabron.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Fabron.Schedulers;

internal interface ICronScheduler : IGrainWithStringKey
{
    [ReadOnly]
    ValueTask<CronTimer?> GetState();

    Task<CronTimer> Schedule(
        string? data,
        CronTimerSpec spec,
        string? owner,
        Dictionary<string, string>? extensions
    );

    Task Start();
    Task Stop();
    Task Delete();
}

internal class CronScheduler : SchedulerGrain<CronTimer>, IGrainBase, ICronScheduler
{
    private readonly SchedulerOptions _options;

    public CronScheduler(
        IGrainContext context,
        IGrainRuntime runtime,
        ILogger<CronScheduler> logger,
        IOptions<SchedulerOptions> options,
        ISystemClock clock,
        ICronTimerStore store,
        IFireDispatcher dispatcher) : base(context, runtime, logger, clock, options.Value, store, dispatcher) => _options = options.Value;

    async Task IGrainBase.OnActivateAsync(CancellationToken cancellationToken)
    {
        _key = this.GetPrimaryKeyString();
        var entry = await _store.GetAsync(_key);
        _state = entry?.State;
        _eTag = entry?.ETag;
    }

    public Task Start() => StartTicker();

    public Task Stop() => StopTicker();

    public Task Delete() => DeleteInternal();

    public ValueTask<CronTimer?> GetState() => new(_state);

    public async Task<CronTimer> Schedule(
        string? data,
        CronTimerSpec spec,
        string? owner,
        Dictionary<string, string>? extensions)
    {
        var utcNow = _clock.UtcNow;
        _state = new CronTimer
        (
            Metadata: new(
                Key: _key,
                CreationTimestamp: utcNow,
                DeletionTimestamp: null,
                Owner: owner,
                Extensions: extensions ?? new()),
            Data: data,
            Spec: spec
        );
        _eTag = await _store.SetAsync(_state, _eTag);

        await StartTicker();
        return _state;
    }

    private Task StartTicker()
    {
        Guard.IsNotNull(_state);
        var utcNow = _clock.UtcNow;
        var notBefore = _state.Spec.NotBefore;
        return notBefore switch
        {
            not null when notBefore.Value > utcNow => Tick(notBefore.Value),
            _ => Tick(default)
        };
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
        if (now > _state.Spec.NotAfter)
        {
            TickerLog.UnexpectedTick(_logger, _key, expectedTickTime, "Expired");
            await StopTicker();
            return;
        }
        if (_state.Spec.NotBefore.HasValue && now < _state.Spec.NotBefore.Value)
        {
            TickerLog.UnexpectedTick(_logger, _key, expectedTickTime, "NotStarted");
            await TickAfter(now, _state.Spec.NotBefore.Value);
            return;
        }

        var cron = Cronos.CronExpression.Parse(_state.Spec.Schedule, _options.CronFormat);



        var from = shouldDispatchForCurrentTick ? expectedTickTime : now;
        var to = from.AddMinutes(2);
        if (to > _state.Spec.NotAfter)
        {
            to = _state.Spec.NotAfter.Value;
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
        if (!nextTick.HasValue || (_state.Spec.NotAfter.HasValue && nextTick.Value > _state.Spec.NotAfter.Value))
        {
            // no more next tick
            await StopTicker();
            return;
        }
        else
        {
            await TickAfter(now, nextTick.Value);
        }
    }

    private void Dispatch(DateTimeOffset schedule)
    {
        Guard.IsNotNull(_state, nameof(_state));
        var now = _clock.UtcNow;
        var dueTime = schedule > now ? schedule.Subtract(now) : TimeSpan.Zero;
        var envelop = _state.ToEnvelop(schedule);
        _runtime.TimerRegistry.RegisterTimer(
            GrainContext,
            obj => DispatchNew((FireEnvelop)obj),
            envelop,
            dueTime,
            Timeout.InfiniteTimeSpan);

        TickerLog.TimerSet(_logger, _key, dueTime, schedule);
    }
}
