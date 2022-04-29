using System;
using System.Threading.Tasks;
using Fabron.FunctionalTests.Commands;
using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests.JobTests
{
    public class ScheduleJobTests : TestBase
    {
        public ScheduleJobTests(DefaultClusterFixture fixture, ITestOutputHelper output) : base(fixture, output)
        { }

        [Fact]
        public async Task ScheduleAndGet()
        {
            var job = await JobManager.ScheduleJob<NoopCommand, NoopCommandResult>(
                name: nameof(ScheduleJobTests),
                @namespace: nameof(ScheduleAndGet),
                command: new NoopCommand(),
                DateTimeOffset.UtcNow.AddMonths(1));

            var scheduledJob = await JobManager.GetJob<NoopCommand, NoopCommandResult>(
                name: nameof(ScheduleJobTests),
                @namespace: nameof(ScheduleAndGet)
            );

            Assert.NotNull(scheduledJob);
        }

    }
}
