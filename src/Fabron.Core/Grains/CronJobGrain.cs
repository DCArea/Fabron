
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fabron.Models;
using Fabron.Store;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Toolkit.Diagnostics;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;
using Orleans.Timers;
using OrleansCodeGen.Orleans.Runtime;

namespace Fabron.Grains;

public interface ICronJobGrain : IGrainWithStringKey
{
    [ReadOnly]
    Task<CronJob?> GetState();

    Task Schedule(
        string cronExp,
        CommandSpec command,
        DateTimeOffset? start,
        DateTimeOffset? end,
        bool suspend,
        Dictionary<string, string>? labels,
        Dictionary<string, string>? annotations);

    Task Trigger();

    Task Suspend();

    Task Resume();

    Task Delete();
}

public partial class CronJobGrain : TickerGrain, IGrainBase, ICronJobGrain
{
    private readonly IGrainRuntime _runtime;
    private readonly ILogger _logger;
    private readonly ISystemClock _clock;
    private readonly CronJobOptions _options;
    private readonly ICronJobStore _store;

    public CronJobGrain(
        IGrainContext context,
        IGrainRuntime runtime,
        ILogger<CronJobGrain> logger,
        ISystemClock clock,
        IOptions<CronJobOptions> options,
        ICronJobStore store) : base(context, runtime, logger, options.Value.TickerInterval)
    {
        _logger = logger;
        _options = options.Value;
        _store = store;
        _clock = clock;
        _runtime = runtime;
    }

    private string _name = default!;
    private string _namespace = default!;
    private CronJob? _job;
    async Task IGrainBase.OnActivateAsync(CancellationToken cancellationToken)
    {
        _key = this.GetPrimaryKeyString();
        var (name, @namespace) = KeyUtils.ParseCronJobKey(_key);
        _name = name;
        _namespace = @namespace;
        _job = await _store.FindAsync(_name, _namespace);
    }

    private DateTimeOffset? _lastSchedule;

    public Task<CronJob?> GetState() => Task.FromResult(_job);

    public async Task Schedule(
        string cronExp,
        CommandSpec command,
        DateTimeOffset? start,
        DateTimeOffset? end,
        bool suspend,
        Dictionary<string, string>? labels,
        Dictionary<string, string>? annotations)
    {
        await TickAfter(_options.TickerInterval);
        _job = new CronJob
        {
            Metadata = new ObjectMetadata
            {
                Name = _name,
                Namespace = _namespace,
                UID = Guid.NewGuid().ToString(),
                CreationTimestamp = _clock.UtcNow,
                DeletionTimestamp = null,
                Labels = labels,
                Annotations = annotations,
                Owner = null
            },
            Spec = new CronJobSpec
            {
                Command = command,
                Schedule = cronExp,
                NotBefore = start,
                ExpirationTime = end,
                Suspend = suspend,
            },
            Status = new CronJobStatus
            { }
        };
        await _store.SaveAsync(_job);

        var now = _clock.UtcNow;
        if (!_job.Spec.Suspend && (_job.Spec.NotBefore is null || _job.Spec.NotBefore.Value <= now))
        {
            if (_options.UseSynchronousTicker)
            {
                await Tick(now);
            }
            else
            {
                _ = Task.Factory.StartNew(() => Tick(now)).Unwrap();
            }
        }
    }

    public Task Trigger()
    {
        return ScheduleNewRun(_clock.UtcNow);
    }

    public async Task Suspend()
    {
        Guard.IsNotNull(_job, nameof(_job));
        _job.Spec.Suspend = true;
        await _store.SaveAsync(_job);
    }

    public async Task Resume()
    {
        Guard.IsNotNull(_job, nameof(_job));
        _job.Spec.Suspend = false;
        await _store.SaveAsync(_job);
    }

    public async Task Delete()
    {
        if (_job is not null)
        {
            await _store.DeleteAsync(_job.Metadata.Name, _job.Metadata.Namespace);
        }
    }

    protected override async Task Tick(DateTimeOffset? expectedTickTime)
    {
        if (_job is null || _job.Deleted || _job.Spec.Suspend)
        {
            await StopTicker();
            return;
        }

        DateTimeOffset now = _clock.UtcNow;
        if (now > _job.Spec.ExpirationTime)
        {
            await StopTicker();
            return;
        }

        if (_job.Spec.NotBefore.HasValue && now < _job.Spec.NotBefore.Value)
        {
            await TickAfter(_job.Spec.NotBefore.Value.Subtract(now));
            return;
        }

        Cronos.CronExpression cron = Cronos.CronExpression.Parse(_job.Spec.Schedule, _options.CronFormat);
        DateTimeOffset? tick;
        DateTimeOffset from = now.AddSeconds(-10);
        if (_lastSchedule.HasValue && _lastSchedule.Value > from)
        {
            from = _lastSchedule.Value;
        }
        tick = cron.GetNextOccurrence(from, _options.TimeZone);

        // Completed
        if (tick is null || (_job.Spec.ExpirationTime.HasValue && tick.Value > _job.Spec.ExpirationTime.Value))
        {
            await StopTicker();
            return;
        }

        // Just at the time to schedule new job
        if (tick.Value <= now.AddSeconds(2))
        {
            await ScheduleNewRun(tick.Value);
        }
        else // not at the time
        {
            await TickAfter(tick.Value.Subtract(now));
            if (expectedTickTime.HasValue)
            {
                TickerLog.UnexpectedTick(_logger, _key, expectedTickTime.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            }
            return;
        }
    }

    private async Task ScheduleNewRun(DateTimeOffset schedule)
    {
        Guard.IsNotNull(_job, nameof(_job));

        string runKey = KeyUtils.BuildCronJobItemKey(_name, _namespace, schedule);
        Log.SchedulingNewRun(_logger, _key, runKey);

        IJobGrain grain = _runtime.GrainFactory.GetGrain<IJobGrain>(runKey);
        var labels = _job.Metadata.Labels;
        var annotations = _job.Metadata.Annotations;
        await grain.Schedule(
            schedule,
            _job.Spec.Command,
            labels,
            annotations,
            new OwnerReference
            {
                Kind = "CronJob",
                Name = _name
            });

        _lastSchedule = schedule;
        Log.ScheduledNewRun(_logger, _key, runKey);
    }

    public static partial class Log
    {
        [LoggerMessage(
            EventId = 10004,
            Level = LogLevel.Debug,
            Message = "[{key}]: Scheduling new run[{runKey}]")]
        public static partial void SchedulingNewRun(ILogger logger, string key, string runKey);

        [LoggerMessage(
            EventId = 10005,
            Level = LogLevel.Information,
            Message = "[{key}]: Scheduled new run[{runKey}]")]
        public static partial void ScheduledNewRun(ILogger logger, string key, string runKey);
    }

}
