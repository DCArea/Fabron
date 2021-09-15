using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron.FunctionalTests.Commands;
using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests.JobTests
{
    public class ScheduleJobTests : TestBase
    {
        public ScheduleJobTests(DefaultClusterFixture fixture, ITestOutputHelper output) : base(fixture, output)
        {
        }

        [Fact(Skip = "Skip")]
        public async Task ShouldCanBeQueried()
        {
            var labels = new Dictionary<string, string>
            {
                {"foo", "bar" }
            };
            Contracts.Job<NoopCommand, NoopCommandResult> job = await JobManager.ScheduleJob<NoopCommand, NoopCommandResult>(
                nameof(ScheduleJobTests) + "/" + nameof(ShouldCanBeQueried),
                new NoopCommand(),
                null,
                labels,
                null);

            IEnumerable<Contracts.Job<NoopCommand, NoopCommandResult>> queried = await JobManager.GetJobByLabel<NoopCommand, NoopCommandResult>("foo", "bar");

            Assert.NotEmpty(queried);
        }
    }
}
