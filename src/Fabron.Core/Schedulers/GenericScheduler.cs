using CommunityToolkit.Diagnostics;
using Fabron.Diagnostics;
using Fabron.Dispatching;
using Fabron.Models;
using Fabron.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Fabron.Schedulers;

public interface IGenericScheduler : IGrainWithStringKey
{
    [ReadOnly]
    Task<TickerStatus> GetTickerStatus();

    [ReadOnly]
    ValueTask<GenericTimer?> GetState();

    Task<GenericTimer> Schedule(
        string? data,
        GenericTimerSpec spec,
        string? owner,
        Dictionary<string, string>? extensions
    );

    Task Start();
    Task Stop();
    Task Delete();
}

public partial class GenericScheduler : SchedulerGrain<GenericTimer>, IGrainBase, IGenericScheduler
{
    public GenericScheduler(
        IGrainContext context,
        IGrainRuntime runtime,
        ILogger<GenericScheduler> logger,
        IOptions<SchedulerOptions> options,
        ISystemClock clock,
        IGenericTimerStore store,
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
        {
            Metadata = new()
            {
                Key = _key,
                CreationTimestamp = utcNow,
                Owner = owner,
                Extensions = extensions ?? new()
            },
            Data = data,
            Spec = spec
        };
        _eTag = await _store.SetAsync(_state, _eTag);

        await StartTicker();
        Telemetry.TimerScheduled.Add(1);
        return _state;
    }

    private Task StartTicker()
    {
        Guard.IsNotNull(_state);
        var utcNow = _clock.UtcNow;
        var schedule_ = _state.Spec.Schedule;
        if (schedule_ <= utcNow)
        {
            Log.TickingForPast(_logger, _key, schedule_);
            return Tick(utcNow);
        }
        else
        {
            return TickAfter(schedule_ - utcNow);
        }
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

    internal static partial class Log
    {
        [LoggerMessage(
            Level = LogLevel.Warning,
            Message = "[{key}]: Schedule {schedule} is in the past, but still ticking now.")]
        public static partial void TickingForPast(ILogger logger, string key, DateTimeOffset schedule);
    }
}

