
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
            Fabron.Models.Job state = new(
                new JobMetadata("test", Guid.NewGuid().ToString(), now, new(), new()),
                new(now.AddMilliseconds(-20), "test", "123"),
                JobStatus.Initial,
                0
            );
            Assert.Equal(TimeSpan.Zero, state.DueTime);
        }
    }
}
