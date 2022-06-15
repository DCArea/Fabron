using System;
using Fabron.Schedulers;

namespace Fabron.Core.Test
{
    public class FakeSystemClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }
}
