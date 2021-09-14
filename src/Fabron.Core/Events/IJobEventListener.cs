using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Fabron.Events
{
    public interface IJobEventListener
    {
        Task On(string jobId, DateTime timestamp, IJobEvent @event);
    }

    public interface ICronJobEventListener
    {
        Task On(string cronJobId, DateTime timestamp, ICronJobEvent @event);
    }


    public class NoopJobEventListener : IJobEventListener
    {
        public Task On(string jobId, DateTime timestamp, IJobEvent @event) => Task.CompletedTask;
    }

    public class LogBasedJobEventListener : IJobEventListener, ICronJobEventListener
    {
        private readonly ILogger _logger;

        public LogBasedJobEventListener(ILogger<LogBasedJobEventListener> logger)
        {
            _logger = logger;
        }

        public Task On(string jobId, DateTime timestamp, IJobEvent @event)
        {
            _logger.LogInformation($"{@event.GetType().Name} on Job[{jobId}] at {timestamp}");
            return Task.CompletedTask;
        }

        public Task On(string cronJobId, DateTime timestamp, ICronJobEvent @event)
        {
            _logger.LogInformation($"{@event.GetType().Name} on CronJob[{cronJobId}] at {timestamp}");
            return Task.CompletedTask;
        }
    }
}
