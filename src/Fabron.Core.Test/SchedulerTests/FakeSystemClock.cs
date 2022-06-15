using System;

namespace Fabron.Core.Test
{
    public class FakeSystemClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }
}
