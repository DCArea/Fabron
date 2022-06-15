using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orleans.Runtime;
using Orleans.Timers;

namespace Fabron.Core.Test.SchedulerTests
{
    public class FakeTimerRegistry : ITimerRegistry
    {
        public List<FakeTimer> Timers { get; } = new();
        public IDisposable RegisterTimer(IGrainContext grainContext, Func<object, Task> asyncCallback, object state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new FakeTimer(asyncCallback, state, dueTime, period);
            Timers.Add(timer);
            return timer;
        }
    }

    public record FakeTimer(
        Func<object, Task> AsyncCallback,
        object State,
        TimeSpan DueTime,
        TimeSpan Period) : IDisposable
    {
        public void Dispose() => throw new NotImplementedException();
    }
}
