using CommunityToolkit.Diagnostics;
using Fabron.Diagnostics;
using Fabron.Dispatching;
using Fabron.Models;
using Fabron.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Orleans.Timers;

namespace Fabron.Schedulers;

internal abstract class SchedulerGrain<TState> : IRemindable
    where TState : class, IDistributedTimer
{
    private static readonly TimeSpan MaxSupportedTimeout = TimeSpan.FromDays(49);
    protected readonly ILogger _logger;
    protected readonly ISystemClock _clock;
    private readonly SchedulerOptions _options;
    private readonly IStateStore<TState> _store;
    private readonly IFireDispatcher _dispatcher;
    private readonly IReminderRegistry _reminderRegistry;

    public SchedulerGrain(
        IGrainContext context,
        IGrainRuntime runtime,
        ILogger logger,
        ISystemClock clock,
        SchedulerOptions options,
        IStateStore<TState> store,
        IFireDispatcher dispatcher)
    {
        GrainContext = context;
        _runtime = runtime;
        _logger = logger;
        _clock = clock;
        _options = options;
        _store = store;
        _dispatcher = dispatcher;
        _reminderRegistry = GrainContext.ActivationServices.GetRequiredService<IReminderRegistry>();
    }

    public IGrainContext GrainContext { get; }
    protected IGrainRuntime _runtime;
    protected string _key = default!;
    protected TState? _state = default!;
    protected string? _eTag = default!;
    private IGrainReminder? _tickReminder;

    internal async Task LoadStateAsync()
    {
        //using var _ = Telemetry.ActivitySource.StartActivity("Load State");
        var entry = await _store.GetAsync(_key);
        _state = entry?.State;
        _eTag = entry?.ETag;
    }

    protected async Task SaveStateAsync()
    {
        Guard.IsNotNull(_state);
        //using var _ = Telemetry.ActivitySource.StartActivity("Save State");
        _eTag = await _store.SetAsync(_state, _eTag);
    }

    protected async Task ClearStateAsync()
    {
        Guard.IsNotNull(_state);
        //using var _ = Telemetry.ActivitySource.StartActivity("Clear State");
        await _store.RemoveAsync(_state.Metadata.Key, _eTag);
        _state = null;
        _eTag = null;
    }

    protected async Task SetExtInternal(Dictionary<string, string?> input)
    {
        if (_state is null) return;
        var existed = _state.Metadata.Extensions;
        foreach (var (k, v) in input)
        {
            if (v is null)
            {
                existed.Remove(k);
            }
            else
            {
                existed[k] = v;
            }
        }
        await SaveStateAsync();
    }


    internal abstract Task Tick(DateTimeOffset expectedTickTime);

    protected async Task TickAfter(DateTimeOffset now, DateTimeOffset tickTime, bool isIntermediary = false)
    {
        var dueTime = tickTime - now;
        // should not happened
        if (dueTime < TimeSpan.Zero)
        {
            dueTime = TimeSpan.Zero;
        }

        if (dueTime > MaxSupportedTimeout)
        {
            dueTime = MaxSupportedTimeout;
        }

        _tickReminder = await _reminderRegistry.RegisterOrUpdateReminder(GrainContext.GrainId, Names.TickerReminder, dueTime, _options.TickerInterval);
        TickerLog.TickerRegistered(_logger, _key, tickTime, dueTime);

        if (!isIntermediary && _state is not null)
        {
            _state.Status.StartedAt = now;
            _state.Status.NextTick = tickTime;
            await SaveStateAsync();
        }
    }

    protected async Task DeleteInternal()
    {
        if (_state is not null)
        {
            await ClearStateAsync();
            await StopTicker();
        }
    }

    protected async Task StopTicker()
    {
        var retry = 0;
        while (true)
        {
            _tickReminder ??= await _reminderRegistry.GetReminder(GrainContext.GrainId, Names.TickerReminder);
            if (_tickReminder is null)
            {
                break;
            }

            if (_state is not null)
            {
                _state.Status.StartedAt = null;
                _state.Status.NextTick = null;
                await SaveStateAsync();
            }

            try
            {
                await _reminderRegistry.UnregisterReminder(GrainContext.GrainId, _tickReminder);
                _tickReminder = null;

                TickerLog.TickerDisposed(_logger, _key);
                // should not deactive since there may be ongoing timers to fire
                // _runtime.DeactivateOnIdle(GrainContext);
                break;
            }
            catch (ReminderException)
            {
                if (retry++ < 3)
                {
                    TickerLog.RetryUnregisterReminder(_logger, _key);
                    continue;
                }
                throw;
            }
            catch (OperationCanceledException)
            {
                // ReminderService has been stopped
                // TODO: add log
                return;
            }
        }

    }

    protected void FireAfter(FireEnvelop envelop, TimeSpan dueTime)
    {
        _runtime.TimerRegistry.RegisterGrainTimer(
            GrainContext,
            (obj, ct) => DispatchNew(obj),
            envelop,
            new GrainTimerCreationOptions { DueTime = dueTime, Period = Timeout.InfiniteTimeSpan });
        TickerLog.TimerSet(_logger, _key, dueTime, envelop.Time);
    }

    protected async Task DispatchNew(FireEnvelop envelop, bool forceDispatch = false)
    {
        Guard.IsNotNull(_state, nameof(_state));
        if (!forceDispatch && _state.Status.StartedAt == null)
        {
            // ticker stopped, ignore
            // TODO: avoid dead fire running if timer is re-scheduled
            // TickerLog.FireCancelled(_logger, _key, envelop.Time.ToUnixTimeSeconds());
            // return;
        }

        var utcNow = _clock.UtcNow;
        Telemetry.OnFabronTimerDispatching(_logger, _key, envelop, utcNow);

        var sw = ValueStopwatch.StartNew();
        try
        {
            await _dispatcher.DispatchAsync(envelop);
            Telemetry.OnFabronTimerDispatched(sw.GetElapsedTime());
        }
        catch (Exception e)
        {
            Telemetry.OnFabronTimerDispatchFailed(_logger, _key, e);
            return;
        }
    }

    async Task IRemindable.ReceiveReminder(string reminderName, TickStatus status)
    {
        TickerLog.ReceivedReminder(_logger, _key, status);
        _tickReminder ??= await _reminderRegistry.GetReminder(GrainContext.GrainId, Names.TickerReminder);

        using var activity = Telemetry.OnTicking();
        if (_state is not null)
        {
            var utcNow = _clock.UtcNow;
            var nextTick = _state.Status.NextTick;
            if (nextTick.HasValue && utcNow.AddMilliseconds(200) < nextTick.Value) // re-ticking
            {
                TickerLog.IntermediaryTickerFired(_logger, _key, nextTick.Value, utcNow);
                await TickAfter(utcNow, nextTick.Value, true);
                return;
            }
        }
        await Tick(_state?.Status.NextTick ?? _clock.UtcNow);
    }
}
