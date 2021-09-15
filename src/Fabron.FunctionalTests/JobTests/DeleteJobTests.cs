using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fabron.FunctionalTests.Commands;
using Fabron.Mando;
using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests.JobTests
{
    public class DeleteJobTests : TestBase
    {

        public DeleteJobTests(DefaultClusterFixture fixture, ITestOutputHelper output) : base(fixture, output) { }

        [Fact(Skip = "Skip")]
        public async Task DeleteJob()
        {
            var labels = new Dictionary<string, string>
            {
                {"foo", "bar" }
            };
            Contracts.Job<NoopCommand, NoopCommandResult> job = await JobManager.ScheduleJob<NoopCommand, NoopCommandResult>(
                nameof(DeleteJobTests) + "/" + nameof(DeleteJob),
                new NoopCommand(),
                null,
                labels,
                null);
            await JobManager.DeleteJobById(job.Metadata.Uid);

            //await Task.Delay(3000);
            Assert.Null(await JobManager.GetJobById<NoopCommand, NoopCommandResult>(job.Metadata.Uid));
            IEnumerable<Contracts.Job<NoopCommand, NoopCommandResult>> queried = await JobManager.GetJobByLabel<NoopCommand, NoopCommandResult>("foo", "bar");
            Assert.Empty(queried);
        }
    }

}
