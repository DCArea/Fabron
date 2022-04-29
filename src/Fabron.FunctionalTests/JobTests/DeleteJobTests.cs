// using System.Collections.Generic;
// using System.Threading;
// using System.Threading.Tasks;
// using Fabron.FunctionalTests.Commands;
// using Fabron.Mando;
// using Fabron.Models;
// using Xunit;
// using Xunit.Abstractions;

// namespace Fabron.FunctionalTests.JobTests
// {
//     public class DeleteJobTests : TestBase
//     {

//         public DeleteJobTests(DefaultClusterFixture fixture, ITestOutputHelper output) : base(fixture, output) { }

//         [Fact]
//         public async Task DeleteJob()
//         {
//             var labels = new Dictionary<string, string>
//             {
//                 {"foo", "bar" }
//             };
//             Contracts.Job<NoopCommand, NoopCommandResult> job = await JobManager.ScheduleJob<NoopCommand, NoopCommandResult>(
//                 nameof(DeleteJobTests) + "/" + nameof(DeleteJob),
//                 new NoopCommand(),
//                 null,
//                 labels,
//                 null);
//             await JobManager.DeleteJob(job.Metadata.Key);

//             Assert.Null(await JobManager.GetJob<NoopCommand, NoopCommandResult>(job.Metadata.Key));
//             IEnumerable<Contracts.Job<NoopCommand, NoopCommandResult>> queried = await JobManager.GetJobByLabel<NoopCommand, NoopCommandResult>("foo", "bar");
//             Assert.Empty(queried);

//             Assert.Null(await JobQuerier.GetJobByKey(job.Metadata.Key));
//         }
//     }

// }
