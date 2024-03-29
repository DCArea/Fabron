﻿using CommunityToolkit.Diagnostics;
using Fabron.Diagnostics;
using Fabron.Dispatching;
using Fabron.Models;
using Fabron.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Placement;
using Orleans.Runtime;

namespace Fabron.Schedulers;

internal interface IGenericScheduler : IGrainWithStringKey, ISchedulerGrain<GenericTimer, GenericTimerSpec>
{ }

[PreferLocalPlacement]
internal sealed class GenericScheduler(
    IGrainContext context,
    IGrainRuntime runtime,
    ILogger<GenericScheduler> logger,
    IOptions<SchedulerOptions> options,
    ISystemClock clock,
    IGenericTimerStore store,
    IFireDispatcher dispatcher) : SchedulerGrain<GenericTimer>(context, runtime, logger, clock, options.Value, store, dispatcher), IGrainBase, IGenericScheduler
{
    Task IGrainBase.OnActivateAsync(CancellationToken cancellationToken)
    {
        _key = this.GetPrimaryKeyString();
        return LoadStateAsync();
    }

    public Task Start() => StartTicker();

    public Task Stop() => StopTicker();

    public Task Delete() => DeleteInternal();

    public ValueTask<GenericTimer?> GetState() => ValueTask.FromResult(_state);

    public async Task<GenericTimer> Schedule(
        string? data,
        GenericTimerSpec spec,
        string? owner,
        Dictionary<string, string>? extensions)
    {
        var utcNow = _clock.UtcNow;
        var schedule_ = spec.Schedule;

        _state = new GenericTimer
        (
            Metadata: new(
                Key: _key,
                CreationTimestamp: utcNow,
                DeletionTimestamp: null,
                Owner: owner,
                Extensions: extensions ?? []),
            Data: data,
            Spec: spec
        );
        await StartTicker();
        Telemetry.TimerScheduled.Add(1);
        return _state;
    }

    public Task SetExt(Dictionary<string, string?> extensions) => SetExtInternal(extensions);

    private Task StartTicker()
    {
        Guard.IsNotNull(_state);
        var utcNow = _clock.UtcNow;
        var schedule_ = _state.Spec.Schedule;
        if (schedule_ <= utcNow)
        {
            Log.TickingForPast(_logger, _key, schedule_);
        }

        return TickAfter(utcNow, schedule_);
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
            TickerLog.UnexpectedTick(_logger, _key, expectedTickTime, "NotExists");
            await StopTicker();
            return;
        }

        var envelop = _state.ToEnvelop(_state.Spec.Schedule);
        await DispatchNew(envelop);
        await StopTicker();
    }

}

