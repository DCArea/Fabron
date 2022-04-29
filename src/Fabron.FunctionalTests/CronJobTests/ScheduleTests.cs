using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron.FunctionalTests.Commands;
using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests.CronJobTests
{
    public class ScheduleTests : TestBase
    {
        private const string Namespace = nameof(CronJobTests) + "." + nameof(ScheduleTests);

        public ScheduleTests(DefaultClusterFixture fixture, ITestOutputHelper output) : base(fixture, output)
        { }

        [Fact]
        public async Task ScheduleAndGet()
        {
            var labels = new Dictionary<string, string>
            {
                {"foo", "bar" }
            };

            Contracts.CronJob<NoopCommand> job = await JobManager.ScheduleCronJob(
                name: nameof(ScheduleAndGet),
                @namespace: Namespace,
                command: new NoopCommand(),
                cronExp: "* * * * *",
                null,
                null,
                false,
                labels,
                null);

            var queried = await JobManager.GetCronJob<NoopCommand>(nameof(ScheduleAndGet), Namespace);

            Assert.NotNull(queried);
        }

    }
}
