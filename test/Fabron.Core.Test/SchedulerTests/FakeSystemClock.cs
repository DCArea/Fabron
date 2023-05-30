using Fabron.Schedulers;

namespace Fabron.Core.Test.SchedulerTests
{
    public class FakeSystemClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; } = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }
}
