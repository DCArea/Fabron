using Fabron.Schedulers;

namespace Fabron.Core.Test.SchedulerTests
{
    public class FakeSystemClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }
}
