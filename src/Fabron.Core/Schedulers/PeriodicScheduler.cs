using CommunityToolkit.Diagnostics;
using Fabron.Dispatching;
using Fabron.Models;
using Fabron.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Fabron.Schedulers;

public interface IPeriodicScheduler : IGrainWithStringKey
{
    [ReadOnly]
    ValueTask<Models.PeriodicTimer?> GetState();

    [ReadOnly]
    Task<TickerStatus> GetTickerStatus();

    Task<Models.PeriodicTimer> Schedule(
        string? data,
        PeriodicTimerSpec spec,
        string? owner,
        Dictionary<string, string>? extensions
    );

    Task Start();
    Task Stop();
    Task Delete();
}

public class PeriodicScheduler : SchedulerGrain<Models.PeriodicTimer>, IGrainBase, IPeriodicScheduler
{
    public PeriodicScheduler(
        IGrainContext context,
        IGrainRuntime runtime,
        ILogger<PeriodicScheduler> logger,
        IOptions<SchedulerOptions> options,
        ISystemClock clock,
        IPeriodicTimerStore store,
        IFireDispatcher dispatcher) : base(context, runtime, logger, clock, options.Value, store, dispatcher) { }

    async Task IGrainBase.OnActivateAsync(CancellationToken cancellationToken)
    {
        _key = GrainContext.GrainReference.GetPrimaryKeyString();
        var entry = await _store.GetAsync(_key);
        _state = entry?.State;
        _eTag = entry?.ETag;
    }

    public async Task Start()
    {
        await StartTicker();
    }

    public async Task Stop()
    {
        await StopTicker();
    }

    public async Task Delete()
    {
        if (_state is not null)
        {
            await _store.RemoveAsync(_state.Metadata.Key, _eTag);
            await StopTicker();
        }
    }

    public ValueTask<Models.PeriodicTimer?> GetState() => new(_state);

    public async Task<Models.PeriodicTimer> Schedule(
        string? data,
        PeriodicTimerSpec spec,
        string? owner,
        Dictionary<string, string>? extensions)
    {
        Guard.IsGreaterThanOrEqualTo(spec.Period, TimeSpan.FromSeconds(5), nameof(spec.Period));

        var utcNow = _clock.UtcNow;
        _state = new Models.PeriodicTimer
        {
            Metadata = new()
            {
                Key = _key,
                CreationTimestamp = utcNow,
                Owner = owner,
                Extensions = extensions ?? new()
            },
            Spec = spec,
            Data = data
        };
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
        if (_state is null || _state.Metadata.DeletionTimestamp is not null)
        {
            TickerLog.UnexpectedTick(_logger, _key, expectedTickTime, "NotRegistered");
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
            var envelop = _state.ToEnvelop(schedule);
            _runtime.TimerRegistry.RegisterTimer(
                GrainContext,
                obj => DispatchNew((FireEnvelop)obj),
                envelop,
                dueTime,
                Timeout.InfiniteTimeSpan);
            dueTime = dueTime.Add(_state.Spec.Period);
            schedule = now.Add(dueTime);
        }
        return schedule;
    }
}
