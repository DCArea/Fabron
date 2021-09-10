using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Fabron.Events
{
    public interface IJobEventListener
    {
        Task On(string jobId, DateTime timestamp, IJobEvent @event);
    }

    public class ConsoleJobEventListener : IJobEventListener
    {
        private readonly ILogger<ConsoleJobEventListener> _logger;

        public ConsoleJobEventListener(ILogger<ConsoleJobEventListener> logger)
        {
            _logger = logger;
        }

        public Task On(string jobId, DateTime timestamp, IJobEvent @event)
        {
            _logger.LogInformation($"{@event.GetType().Name} on Job[{jobId}] at {timestamp}");
            return Task.CompletedTask;
        }
    }
}
