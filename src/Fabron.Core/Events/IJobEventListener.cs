using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Fabron.Events
{
    public interface IJobEventListener
    {
        Task On(string key, DateTime timestamp, IJobEvent @event);
    }

    public interface ICronJobEventListener
    {
        Task On(string key, DateTime timestamp, ICronJobEvent @event);
    }


    public class NoopJobEventListener : IJobEventListener
    {
        public Task On(string key, DateTime timestamp, IJobEvent @event) => Task.CompletedTask;
    }

    public class NoopCronJobEventListener : ICronJobEventListener
    {
        public Task On(string key, DateTime timestamp, ICronJobEvent @event) => Task.CompletedTask;
    }

    public class LogBasedJobEventListener : IJobEventListener, ICronJobEventListener
    {
        private readonly ILogger _logger;

        public LogBasedJobEventListener(ILogger<LogBasedJobEventListener> logger)
        {
            _logger = logger;
        }

        public Task On(string key, DateTime timestamp, IJobEvent @event)
        {
            _logger.LogInformation($"{@event.GetType().Name} on Job[{key}] at {timestamp}");
            return Task.CompletedTask;
        }

        public Task On(string key, DateTime timestamp, ICronJobEvent @event)
        {
            _logger.LogInformation($"{@event.GetType().Name} on CronJob[{key}] at {timestamp}");
            return Task.CompletedTask;
        }
    }
}
