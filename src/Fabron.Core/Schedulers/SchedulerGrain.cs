using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Fabron.CloudEvents;
using Fabron.Diagnostics;
using Fabron.Models;
using Fabron.Store;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Diagnostics;
using Orleans;
using Orleans.Runtime;
using Orleans.Timers;

namespace Fabron.Schedulers;

public abstract class SchedulerGrain<TState> : IRemindable
    where TState : class, IScheduledEvent
{
    protected readonly ILogger _logger;
    protected readonly ISystemClock _clock;
    private readonly SchedulerOptions _options;
    protected readonly IStateStore<TState> _store;
    private readonly IEventDispatcher _dispatcher;
    private readonly IReminderRegistry _reminderRegistry;

    public SchedulerGrain(
        IGrainContext context,
        IGrainRuntime runtime,
        ILogger logger,
        ISystemClock clock,
        SchedulerOptions options,
        IStateStore<TState> store,
        IEventDispatcher dispatcher)
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
    protected Queue<DateTimeOffset> RecentDispatches { get; } = new(20);

    internal abstract Task Tick(DateTimeOffset expectedTickTime);

    protected async Task TickAfter(TimeSpan dueTime)
    {
        if (dueTime < TimeSpan.Zero)
        {
            dueTime = TimeSpan.Zero;
        }
        _tickReminder = await _reminderRegistry.RegisterOrUpdateReminder(GrainContext.GrainId, Names.TickerReminder, dueTime, _options.TickerInterval);
        TickerLog.TickerRegistered(_logger, _key, dueTime);
    }

    protected async Task StopTicker()
    {
        int retry = 0;
        while (true)
        {
            if (_tickReminder is null)
            {
                _tickReminder = await _reminderRegistry.GetReminder(GrainContext.GrainId, Names.TickerReminder);
            }
            if (_tickReminder is null) break;
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
    }

    public async Task<TickerStatus> GetTickerStatus()
    {
        var reminderTable = _runtime.ServiceProvider.GetRequiredService<IReminderTable>();
        var entry = await reminderTable.ReadRow(GrainContext.GrainReference, Names.TickerReminder);
        return new TickerStatus
        {
            NextTick = entry?.StartAt,
            RecentDispatches = RecentDispatches.ToList()
        };
    }

    protected void RecordTick(DateTimeOffset tickTime)
    {
        RecentDispatches.Enqueue(tickTime);
        if (RecentDispatches.Count > 20)
        {
            RecentDispatches.Dequeue();
        }
    }

    protected async Task DispatchNew(CloudEventEnvelop cloudEvent)
    {
        Guard.IsNotNull(_state, nameof(_state));

        var utcNow = _clock.UtcNow;
        RecordTick(utcNow);
        Telemetry.OnCloudEventDispatching(_logger, _key, cloudEvent, utcNow);

        var sw = ValueStopwatch.StartNew();
        try
        {
            await _dispatcher.DispatchAsync(_state.Metadata, cloudEvent);
            Telemetry.OnCloudEventDispatched(sw.GetElapsedTime());
        }
        catch (Exception e)
        {
            Telemetry.OnCloudEventDispatchFailed(_logger, _key, e);
            return;
        }
    }

    async Task IRemindable.ReceiveReminder(string reminderName, TickStatus status)
    {
        TickerLog.ReceivedReminder(_logger, _key, status);
        _tickReminder ??= await _reminderRegistry.GetReminder(GrainContext.GrainId, Names.TickerReminder);

        using var activity = Telemetry.OnTicking();
        await Tick(status.FirstTickTime);
    }
}
