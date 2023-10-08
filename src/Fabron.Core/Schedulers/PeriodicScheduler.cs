using CommunityToolkit.Diagnostics;
using Fabron.Dispatching;
using Fabron.Models;
using Fabron.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;

namespace Fabron.Schedulers;

internal interface IPeriodicScheduler : IGrainWithStringKey, ISchedulerGrain<PeriodicTimer, PeriodicTimerSpec>
{ }

internal sealed class PeriodicScheduler(
    IGrainContext context,
    IGrainRuntime runtime,
    ILogger<PeriodicScheduler> logger,
    IOptions<SchedulerOptions> options,
    ISystemClock clock,
    IPeriodicTimerStore store,
    IFireDispatcher dispatcher) : SchedulerGrain<PeriodicTimer>(context, runtime, logger, clock, options.Value, store, dispatcher), IGrainBase, IPeriodicScheduler
{
    Task IGrainBase.OnActivateAsync(CancellationToken cancellationToken)
    {
        _key = this.GetPrimaryKeyString();
        return LoadStateAsync();
    }

    public Task Start() => StartTicker();

    public Task Stop() => StopTicker();

    public Task Delete() => DeleteInternal();

    public ValueTask<PeriodicTimer?> GetState() => new(_state);

    public async Task<PeriodicTimer> Schedule(
        string? data,
        PeriodicTimerSpec spec,
        string? owner,
        Dictionary<string, string>? extensions)
    {
        Guard.IsGreaterThanOrEqualTo(spec.Period, TimeSpan.FromSeconds(5), nameof(spec.Period));

        var utcNow = _clock.UtcNow;
        _state = new PeriodicTimer
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
        //_eTag = await _store.SetAsync(_state, _eTag);
        await StartTicker();
        return _state;
    }

    public Task SetExt(Dictionary<string, string?> extensions) => SetExtInternal(extensions);

    private Task StartTicker()
    {
        Guard.IsNotNull(_state);
        var utcNow = _clock.UtcNow;
        var notBefore = _state.Spec.NotBefore;
        return notBefore switch
        {
            not null when notBefore.Value > utcNow => Tick(notBefore.Value),
            _ => Tick(utcNow)
        };
    }

    public Task Tick()
    {
        Guard.IsNotNull(_state, nameof(_state));
        var envelop = _state.ToEnvelop(DateTimeOffset.UtcNow);
        return DispatchNew(envelop, true);
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

        var from = expectedTickTime;
        var to = from;
        DateTimeOffset nextTick;
        do
        {
            to = from.AddMinutes(1);
            if (to > _state.Spec.NotAfter)
            {
                to = _state.Spec.NotAfter.Value;
            }
            nextTick = Dispatch(from, to);
            from = nextTick;
        } while (from < now);

        if (_state.Spec.NotAfter.HasValue && nextTick > _state.Spec.NotAfter.Value)
        {
            // no more next tick
            await StopTicker();
            return;
        }
        await TickAfter(now, nextTick);
    }

    private DateTimeOffset Dispatch(DateTimeOffset from, DateTimeOffset to)
    {
        Guard.IsNotNull(_state, nameof(_state));
        var nextTick = from;
        var dueTime = TimeSpan.Zero;
        while (nextTick < to)
        {
            var envelop = _state.ToEnvelop(nextTick);
            FireAfter(envelop, dueTime);
            dueTime = dueTime.Add(_state.Spec.Period);
            nextTick = from.Add(dueTime);
        }
        return nextTick;
    }
}
