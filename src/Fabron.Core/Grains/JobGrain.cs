
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fabron.Mando;
using Fabron.Models;
using Fabron.Store;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Toolkit.Diagnostics;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Fabron.Grains;

public interface IJobGrain : IGrainWithStringKey
{
    [ReadOnly]
    Task<Job?> GetState();

    [ReadOnly]
    Task<JobExecutionStatus> GetStatus();

    Task<Job> Schedule(
        DateTimeOffset? schedule,
        CommandSpec command,
        Dictionary<string, string>? labels,
        Dictionary<string, string>? annotations,
        OwnerReference? owner
    );

    Task Delete();
}

public partial class JobGrain : TickerGrain, IGrainBase, IJobGrain
{
    private readonly IGrainRuntime _runtime;
    private readonly ILogger _logger;
    private readonly JobOptions _options;
    private readonly IMediator _mediator;
    private readonly IJobStore _store;

    public JobGrain(
        IGrainContext context,
        IGrainRuntime runtime,
        ILogger<JobGrain> logger,
        IOptions<JobOptions> options,
        IMediator mediator,
        IJobStore store) : base(context, runtime, logger, options.Value.TickerInterval)
    {
        _logger = logger;
        _options = options.Value;
        _mediator = mediator;
        _store = store;
        _runtime = runtime;
    }

    private Job? _job;
    async Task IGrainBase.OnActivateAsync(CancellationToken cancellationToken)
    {
        _key = GrainContext.GrainReference.GetPrimaryKeyString();
        var (name, @namespace) = KeyUtils.ParseJobKey(_key);
        _job = await _store.FindAsync(name, @namespace);
    }

    public Task<Job?> GetState() => Task.FromResult(_job);

    public Task<JobExecutionStatus> GetStatus()
    {
        if (_job is null)
        {
            throw new InvalidOperationException("Job is not scheduled");
        }
        if (_job.Deleted)
        {
            throw new InvalidOperationException("Job was deleted");
        }
        return Task.FromResult(_job.Status.ExecutionStatus);
    }

    public async Task Delete()
    {
        if (_job is not null)
        {
            await _store.DeleteAsync(_job.Metadata.Name, _job.Metadata.Namespace);
        }
    }

    public async Task<Job> Schedule(
        DateTimeOffset? schedule,
        CommandSpec command,
        Dictionary<string, string>? labels,
        Dictionary<string, string>? annotations,
        OwnerReference? owner)
    {
        var utcNow = DateTimeOffset.UtcNow;
        var schedule_ = schedule is null || schedule.Value < utcNow ? utcNow : schedule.Value;
        await TickAfter(_options.TickerInterval);
        var (name, @namespace) = KeyUtils.ParseJobKey(_key);

        _job = new Job
        {
            Metadata = new ObjectMetadata
            {
                Name = name,
                Namespace = @namespace,
                UID = Guid.NewGuid().ToString(),
                CreationTimestamp = DateTimeOffset.UtcNow,
                DeletionTimestamp = null,
                Labels = labels,
                Annotations = annotations,
                Owner = owner
            },
            Spec = new JobSpec()
            {
                Command = command,
                Schedule = schedule
            },
            Status = new JobStatus()
            {
                ExecutionStatus = JobExecutionStatus.Scheduled
            }
        };
        await _store.SaveAsync(_job);

        utcNow = DateTimeOffset.UtcNow;
        if (schedule_ <= utcNow)
        {
            _ = Task.Factory.StartNew(() => Tick(utcNow)).Unwrap();
        }
        else
        {
            await TickAfter(schedule_ - utcNow);
        }
        return _job;
    }

    protected override async Task Tick(DateTimeOffset? expectedTickTime)
    {
        if (_job is null || _job.Deleted)
        {
            // TODO: add log
            await StopTicker();
            return;
        }

        if (_job.Status.ExecutionStatus == JobExecutionStatus.Scheduled)
        {
            await Start();
        }

        // TODO: invalid state, tring to recover
        if (_job.Status.ExecutionStatus == JobExecutionStatus.Started)
        {
            await Execute();
        }

        // TODO: log inconsistent state
    }

    private async Task Start()
    {
        Guard.IsNotNull(_job, nameof(_job));
        if (_job.Status.ExecutionStatus != JobExecutionStatus.Scheduled)
        {
            // TODO: Log or throw exception
            return;
        }

        _job.Status.ExecutionStatus = JobExecutionStatus.Started;
        await _store.SaveAsync(_job);

        await Execute();
    }

    private async Task Execute()
    {
        Guard.IsNotNull(_job, nameof(_job));
        string? result;
        var sw = ValueStopwatch.StartNew();
        try
        {
            result = await _mediator.Handle(_job.Spec.Command.Name, _job.Spec.Command.Data);

            _job.Status.ExecutionStatus = JobExecutionStatus.Complete;
            _job.Status.Result = result;
        }
        catch (Exception e)
        {
            _job.Status.ExecutionStatus = JobExecutionStatus.Started;
            _job.Status.Reason = "ExceptionOccurred";
            _job.Status.Message = e.ToString();
        }

        MetricsHelper.JobExecutionDuration.Observe(sw.GetElapsedTime().TotalSeconds);
        await _store.SaveAsync(_job);

        await StopTicker();
    }

    public static partial class Log
    {
    }
}

