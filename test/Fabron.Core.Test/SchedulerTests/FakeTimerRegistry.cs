using Orleans.Runtime;
using Orleans.Timers;

namespace Fabron.Core.Test.SchedulerTests
{
    public class FakeTimerRegistry : ITimerRegistry
    {
        public List<FakeTimer> Timers { get; } = [];
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
        public Task Trigger() => AsyncCallback(State);
        public void Dispose() { }
    }
}
