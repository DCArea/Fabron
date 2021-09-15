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
            await JobManager.DeleteCronJobById(job.Metadata.Uid);

            //await Task.Delay(3000);
            Assert.Null(await JobManager.GetCronJobById<NoopCommand>(job.Metadata.Uid));
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
            await JobManager.DeleteCronJobById(job.Metadata.Uid);

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


        //[Fact]
        //public async Task DeleteTwice()
        //{
        //    var labels = new Dictionary<string, string>
        //    {
        //        {"foo", "bar" }
        //    };
        //    Contracts.CronJob<NoopCommand> job = await JobManager.ScheduleCronJob<NoopCommand>(
        //        nameof(DeleteJobTests) + "/" + nameof(DeleteJob),
        //        "* * * * *",
        //        new NoopCommand(),
        //        null,
        //        null,
        //        false,
        //        null,
        //        null);
        //    Grains.ICronJobGrain? grain = GetCronJobGrain(job.Metadata.Uid);
        //    await JobManager.DeleteCronJobById(job.Metadata.Uid);

        //    while (true)
        //    {
        //        if (!await grain.IsPurged())
        //        {
        //            await Task.Delay(10);
        //        }
        //        else { break; }
        //    }


        //    await JobManager.DeleteCronJobById(job.Metadata.Uid);
        //}
    }

}
