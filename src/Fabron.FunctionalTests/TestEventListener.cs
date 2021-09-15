
using System;
using System.Threading.Tasks;
using Fabron.Events;

namespace Fabron.FunctionalTests
{
    public class BlockedEventListener: IJobEventListener, ICronJobEventListener
    {
        public Task On(string jobId, DateTime timestamp, IJobEvent @event)
        {
            return Task.Delay(TimeSpan.MaxValue);
        }

        public Task On(string cronJobId, DateTime timestamp, ICronJobEvent @event)
        {
            return Task.Delay(TimeSpan.MaxValue);
        }
    }
}
