
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabron.Grains;
using Fabron.Models;
using Fabron.Models.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Fabron
{
    public class DefaultJobEventBus : IJobEventBus
    {
        private readonly IGrainFactory _factory;
        private readonly IServiceProvider _provider;

        public DefaultJobEventBus(IGrainFactory factory, IServiceProvider provider)
        {
            _factory = factory;
            _provider = provider;
        }

        public Task OnJobStateChanged(Job jobState)
            => Send(new JobStateChanged(jobState.Metadata.Uid));

        public Task OnCronJobStateChanged(CronJob jobState)
            => Send(new CronJobStateChanged(jobState.Metadata.Uid));

        public Task OnJobExecutionFailed(Job jobState, string reason)
            => Send(new JobExecutionFailed(jobState.Metadata.Uid, reason));

        private Task Send<TJobEvent>(TJobEvent @event) where TJobEvent : IJobEvent
        {
            IEnumerable<IJobEventHandler<TJobEvent>> handlers = _provider.GetServices<IJobEventHandler<TJobEvent>>();
            IEnumerable<Task> tasks = handlers.Select(handler => handler.On(@event));
            return Task.WhenAll(tasks);
        }
    }

    public class DefaultJobStateChangedHandler :
        IJobEventHandler<JobStateChanged>,
        IJobEventHandler<CronJobStateChanged>,
        IJobEventHandler<JobExecutionFailed>
    {
        private readonly IGrainFactory _factory;
        private readonly ILogger _failedExecutionLogger;

        public DefaultJobStateChangedHandler(IGrainFactory factory, ILoggerFactory logger)
        {
            _factory = factory;
            _failedExecutionLogger = logger.CreateLogger("Fabron.Events.JobExecutionFailed");
        }

        public Task On(JobStateChanged @event)
            => _factory.GetGrain<IBatchJobReporterWorker>(0)
                .ReportJob(@event.JobId);

        public Task On(CronJobStateChanged @event)
            => _factory.GetGrain<IBatchJobReporterWorker>(0)
                .ReportCronJob(@event.CronJobId);

        public Task On(JobExecutionFailed @event)
        {
            _failedExecutionLogger.LogWarning($"Job[{@event.JobId}] Exection Failed, Reason: {@event.Reason}");
            return Task.CompletedTask;
        }
    }
}
