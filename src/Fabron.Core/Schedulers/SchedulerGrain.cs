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

namespace Fabron.Schedulers;

public abstract class SchedulerGrain<TState> : IRemindable
    where TState : class, IScheduledEvent
{
    protected readonly ILogger _logger;
    protected readonly ISystemClock _clock;
    private readonly SchedulerOptions _options;
    protected readonly IStateStore<TState> _store;
    private readonly IEventDispatcher _dispatcher;

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
        _tickReminder = await _runtime.ReminderRegistry.RegisterOrUpdateReminder(GrainContext.GrainId, Names.TickerReminder, dueTime, _options.TickerInterval);
        TickerLog.TickerRegistered(_logger, _key, dueTime);
    }

    protected async Task StopTicker()
    {
        int retry = 0;
        while (true)
        {
            if (_tickReminder is null)
            {
                _tickReminder = await _runtime.ReminderRegistry.GetReminder(GrainContext.GrainId, Names.TickerReminder);
            }
            if (_tickReminder is null) break;
            try
            {
                await _runtime.ReminderRegistry.UnregisterReminder(GrainContext.GrainId, _tickReminder);
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
        Meters.RecordCloudEventDispatchTardiness(utcNow, cloudEvent.Time);
        TickerLog.Dispatching(_logger, _key, utcNow, cloudEvent.Time);

        var sw = ValueStopwatch.StartNew();
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

    async Task IRemindable.ReceiveReminder(string reminderName, TickStatus status)
    {
        TickerLog.ReceivedReminder(_logger, _key, status);
        if (_tickReminder is null)
        {
            _tickReminder = await _runtime.ReminderRegistry.GetReminder(GrainContext.GrainId, Names.TickerReminder);
        }

        Activity.Current?.Dispose();
        Activity.Current = null;
        using var activity = Activities.Source.StartActivity("Ticking");
        await Tick(status.FirstTickTime);
    }
}
