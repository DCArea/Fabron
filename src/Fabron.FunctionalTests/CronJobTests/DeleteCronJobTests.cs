using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron.FunctionalTests.Commands;
using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests.JobTests
{
    public class DeleteCronJobTests : TestBase
    {

        public DeleteCronJobTests(DefaultClusterFixture fixture, ITestOutputHelper output) : base(fixture, output) { }

        [Fact(Skip = "Skip")]
        public async Task DeleteJob()
        {
            var labels = new Dictionary<string, string>
            {
                {"foo", "bar" }
            };
            Contracts.CronJob<NoopCommand> job = await JobManager.ScheduleCronJob<NoopCommand>(
                nameof(DeleteCronJobTests) + "/" + nameof(DeleteJob),
                "* * * * *",
                new NoopCommand(),
                null,
                null,
                false,
                null,
                null);
            await JobManager.DeleteCronJob(job.Metadata.Key);

            //await Task.Delay(3000);
            Assert.Null(await JobManager.GetCronJob<NoopCommand>(job.Metadata.Key));
            IEnumerable<Contracts.CronJob<NoopCommand>>? queried = await JobManager.GetCronJobByLabel<NoopCommand>("foo", "bar");
            Assert.Empty(queried);
        }

        [Fact(Skip = "Skip")]
        public async Task DeleteAndSchedule()
        {
            var labels = new Dictionary<string, string>
            {
                {"foo", "bar" }
            };
            Contracts.CronJob<NoopCommand> job = await JobManager.ScheduleCronJob<NoopCommand>(
                nameof(DeleteCronJobTests) + "/" + nameof(DeleteAndSchedule),
                "* * * * *",
                new NoopCommand(),
                null,
                null,
                false,
                null,
                null);
            await JobManager.DeleteCronJob(job.Metadata.Key);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
             JobManager.ScheduleCronJob<NoopCommand>(
                nameof(DeleteJobTests) + "/" + nameof(DeleteJob),
                "* * * * *",
                new NoopCommand(),
                null,
                null,
                false,
                null,
                null)
            );
        }


        [Fact(Skip = "SKIP")]
        public async Task DeleteTwice()
        {
            var labels = new Dictionary<string, string>
            {
                {"foo", "bar" }
            };
            Contracts.CronJob<NoopCommand> job = await JobManager.ScheduleCronJob<NoopCommand>(
                nameof(DeleteCronJobTests) + "/" + nameof(DeleteTwice),
                "* * * * *",
                new NoopCommand(),
                null,
                null,
                false,
                null,
                null);
            Grains.ICronJobGrain? grain = GetCronJobGrain(job.Metadata.Key);
            await JobManager.DeleteCronJob(job.Metadata.Key);

            await grain.Purge();

            await JobManager.DeleteCronJob(job.Metadata.Key);
        }
    }

}
