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
    protected readonly IStateStore<TState> _store;
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
    //protected Queue<DateTimeOffset> RecentDispatches { get; } = new(20);

    internal abstract Task Tick(DateTimeOffset expectedTickTime);

    protected async Task TickAfter(DateTimeOffset now, DateTimeOffset tickTime, bool isIntermediary = false)
    {
        // should not happened
        TimeSpan dueTime = tickTime - now;
        if (dueTime < TimeSpan.Zero)
        {
            dueTime = TimeSpan.Zero;
        }

        if (dueTime > MaxSupportedTimeout)
        {
            dueTime = MaxSupportedTimeout;
        }

        _tickReminder = await _reminderRegistry.RegisterOrUpdateReminder(GrainContext.GrainId, Names.TickerReminder, dueTime, _options.TickerInterval);
        TickerLog.TickerRegistered(_logger, _key, dueTime);

        if (!isIntermediary && _state is not null)
        {
            _state.Status.StartedAt = now;
            _state.Status.NextTick = tickTime;
            _eTag = await _store.SetAsync(_state, _eTag);
        }
    }

    protected async Task DeleteInternal()
    {
        if (_state is not null)
        {
            await _store.RemoveAsync(_state.Metadata.Key, _eTag);
            await StopTicker();
            _state = null;
            _eTag = null;
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

            try
            {
                await _reminderRegistry.UnregisterReminder(GrainContext.GrainId, _tickReminder);
                _tickReminder = null;

                TickerLog.TickerDisposed(_logger, _key);
                _runtime.DeactivateOnIdle(GrainContext);
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

        if (_state is not null)
        {
            _state.Status.StartedAt = null;
            _state.Status.NextTick = null;
        }
    }

    protected void RecordTick(DateTimeOffset tickTime)
    {
        var recentTicks = _state!.Status.RecentTicks;
        recentTicks.Enqueue(tickTime);
        if (recentTicks.Count > 20)
        {
            recentTicks.Dequeue();
        }
    }

    protected async Task DispatchNew(FireEnvelop envelop)
    {
        Guard.IsNotNull(_state, nameof(_state));

        var utcNow = _clock.UtcNow;
        RecordTick(utcNow);
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
            if (nextTick.HasValue && utcNow < nextTick.Value) // re-ticking
            {
                await TickAfter(utcNow, nextTick.Value, true);
                return;
            }
        }
        await Tick(status.FirstTickTime);
    }
}
