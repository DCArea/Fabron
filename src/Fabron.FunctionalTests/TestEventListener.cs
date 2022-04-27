
using System;
using System.Threading.Tasks;
using Fabron.Events;

namespace Fabron.FunctionalTests
{
    public class BlockedEventListener : IJobEventListener, ICronJobEventListener
    {
        public Task On(string key, DateTime timestamp, IJobEvent @event)
        {
            return Task.Delay(TimeSpan.MaxValue);
        }

        public Task On(string key, DateTime timestamp, ICronJobEvent @event)
        {
            return Task.Delay(TimeSpan.MaxValue);
        }
    }
}
