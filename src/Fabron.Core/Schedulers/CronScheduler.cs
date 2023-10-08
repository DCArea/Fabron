using CommunityToolkit.Diagnostics;
using Cronos;
using Fabron.Dispatching;
using Fabron.Models;
using Fabron.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;

namespace Fabron.Schedulers;

internal interface ICronScheduler : IGrainWithStringKey, ISchedulerGrain<CronTimer, CronTimerSpec>
{ }

internal sealed class CronScheduler(
    IGrainContext context,
    IGrainRuntime runtime,
    ILogger<CronScheduler> logger,
    IOptions<SchedulerOptions> options,
    ISystemClock clock,
    ICronTimerStore store,
    IFireDispatcher dispatcher) : SchedulerGrain<CronTimer>(context, runtime, logger, clock, options.Value, store, dispatcher), IGrainBase, ICronScheduler
{
    private readonly SchedulerOptions _options = options.Value;

    Task IGrainBase.OnActivateAsync(CancellationToken cancellationToken)
    {
        _key = this.GetPrimaryKeyString();
        return LoadStateAsync();
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
        await SaveStateAsync();
        await StartTicker();
        return _state;
    }

    public Task SetExt(Dictionary<string, string?> extensions) => SetExtInternal(extensions);

    private Task StartTicker()
    {
        Guard.IsNotNull(_state);
        var utcNow = _clock.UtcNow;
        if (_state.Spec.NotAfter is { } notAfter && utcNow > notAfter)
        {
            return Task.CompletedTask;
        }
        var from = _state.Spec.NotBefore is { } nb && nb > utcNow
            ? nb
            : utcNow;
        var cron = CronExpression.Parse(_state.Spec.Schedule, _options.CronFormat);
        var nextTick = cron.GetNextOccurrence(from, _options.TimeZone, inclusive: true);
        if (nextTick == null)
        {
            // could this happen?
            return Task.CompletedTask;
        }
        return TickAfter(utcNow, nextTick.Value);
    }

    public Task Tick()
    {
        Guard.IsNotNull(_state, nameof(_state));
        var envelop = _state.ToEnvelop(DateTimeOffset.UtcNow);
        return DispatchNew(envelop, true);
    }

    internal override async Task Tick(DateTimeOffset expectedTickTime)
    {
        var now = _clock.UtcNow;
        TickerLog.Ticking(_logger, _key, now, expectedTickTime);

        if (now.Subtract(expectedTickTime) > TimeSpan.FromMinutes(2))
        {
            TickerLog.UnexpectedTick(_logger, _key, expectedTickTime, "Missed");
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

        if (_state.Spec.NotBefore is { } notBefore && now < notBefore)
        {
            TickerLog.UnexpectedTick(_logger, _key, expectedTickTime, "NotStarted");
            await StartTicker();
            return;
        }

        var from = expectedTickTime;
        var to = from;

        var cron = CronExpression.Parse(_state.Spec.Schedule, _options.CronFormat);

        do
        {
            to = from.AddMinutes(2);
            if (to > _state.Spec.NotAfter)
            {
                to = _state.Spec.NotAfter.Value;
            }

            var schedules = cron.GetOccurrences(from, to, _options.TimeZone, fromInclusive: true, toInclusive: false);
            foreach (var schedule in schedules)
            {
                Dispatch(schedule);
            }
            from = to;
        } while (from < now);

        var next = to;
        var nextTick = cron.GetNextOccurrence(next, _options.TimeZone, inclusive: true);
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
        TimeSpan dueTime;
        if (schedule > now)
        {
            dueTime = schedule.Subtract(now);
        }
        else
        {
            dueTime = TimeSpan.Zero;
            var delayTime = now.Subtract(schedule);
            if (delayTime.TotalMinutes > 2)
            {
                TickerLog.FireDelayed(_logger, _key, schedule, delayTime);
            }
        }
        var envelop = _state.ToEnvelop(schedule);
        FireAfter(envelop, dueTime);
    }
}
