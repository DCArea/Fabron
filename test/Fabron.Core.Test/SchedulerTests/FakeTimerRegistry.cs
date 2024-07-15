using Orleans.Runtime;
using Orleans.Timers;

namespace Fabron.Core.Test.SchedulerTests
{
    public class FakeTimerRegistry : ITimerRegistry
    {
        public List<FakeGrainTimer> Timers { get; } = [];

        public IGrainTimer RegisterGrainTimer<TState>(IGrainContext grainContext, Func<TState, CancellationToken, Task> callback, TState state, GrainTimerCreationOptions options)
        {
            var timer = new FakeGrainTimer((obj, ct) => callback(state, ct), state, options);
            Timers.Add(timer);
            return timer;
        }

        public IDisposable RegisterTimer(IGrainContext grainContext, Func<object?, Task> asyncCallback, object? state, TimeSpan dueTime, TimeSpan period) => throw new NotImplementedException();
    }

    public class FakeGrainTimer(
        Func<object?, CancellationToken, Task> AsyncCallback,
        object? State,
        GrainTimerCreationOptions Options) : IGrainTimer
    {
        public TimeSpan DueTime => Options.DueTime;

        public TimeSpan Period => Options.Period;

        public Task Trigger() => AsyncCallback(State, default);
        public void Dispose() { }
        public void Change(TimeSpan dueTime, TimeSpan period) => throw new NotImplementedException();
    }
}
