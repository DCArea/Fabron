using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron.FunctionalTests.Commands;
using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests.CronJobTests
{
    public class TriggerCronJobTests : TestBase
    {
        public TriggerCronJobTests(DefaultClusterFixture fixture, ITestOutputHelper output) : base(fixture, output)
        {
        }

        [Fact(Skip = "Skip")]
        public async Task ShouldScheduleNewJob()
        {
            DateTime utcNow = DateTime.UtcNow;
            var labels = new Dictionary<string, string>
            {
                {"foo", "bar" }
            };
            Contracts.CronJob<NoopCommand>? job = await JobManager.ScheduleCronJob<NoopCommand>(
                nameof(TriggerCronJobTests) + "/" + nameof(ShouldScheduleNewJob),
                $"* * * {utcNow.AddMonths(1).Month} *",
                new NoopCommand(),
                null,
                null,
                false,
                labels,
                null);

            await JobManager.TriggerCronJob(job.Metadata.Uid);
            IEnumerable<Contracts.Job<NoopCommand, NoopCommandResult>>? items = await JobManager.GetJobByCron<NoopCommand, NoopCommandResult>(job.Metadata.Uid);
            Assert.NotEmpty(items);
        }
    }
}
