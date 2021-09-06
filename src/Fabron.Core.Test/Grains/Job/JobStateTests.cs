
using System;
using Fabron.Models;

using Xunit;

namespace Core.Test.Grains.Job
{
    public class JobStateTests
    {
        [Fact]
        public void Schedule20msAgo()
        {
            DateTime now = DateTime.UtcNow;
            Fabron.Models.Job state = new()
            {
                Metadata = new JobMetadata("test", now, new()),
                Spec = new(now.AddMilliseconds(-20), "test", "123"),
            };
            Assert.Equal(TimeSpan.Zero, state.DueTime);
        }
    }
}
