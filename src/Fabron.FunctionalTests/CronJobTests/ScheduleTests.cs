using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron.FunctionalTests.Commands;
using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests.CronJobTests
{
    public class ScheduleTests : TestBase
    {
        public ScheduleTests(DefaultClusterFixture fixture, ITestOutputHelper output) : base(fixture, output)
        {
        }

        [Fact(Skip = "Skip")]
        // [Fact]
        public async Task ShouldCanBeQueried()
        {
            var labels = new Dictionary<string, string>
            {
                {"foo", "bar" }
            };

            Contracts.CronJob<NoopCommand> job = await JobManager.ScheduleCronJob<NoopCommand>(
                this.GetType().Name + "/" + nameof(ShouldCanBeQueried),
                "* * * * *",
                new NoopCommand(),
                null,
                null,
                false,
                labels,
                null);
            Grains.ICronJobGrain? grain = GetCronJobGrain(job.Metadata.Key);

            await grain.WaitEventsConsumed(20);

            IEnumerable<Contracts.CronJob<NoopCommand>> queried = await JobManager.GetCronJobByLabel<NoopCommand>("foo", "bar");
            Assert.NotEmpty(queried);
        }
    }
}
