using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron.Events;
using Fabron.FunctionalTests.Commands;
using Fabron.Models;
using Xunit;
using Xunit.Abstractions;
using Fabron.Grains;

namespace Fabron.FunctionalTests.JobTests
{
    public class StateTests : TestBase
    {
        public StateTests(DefaultClusterFixture fixture, ITestOutputHelper output) : base(fixture, output)
        {
        }

        [Fact]
        public async Task RestoreFromQuerier()
        {
            var key = nameof(ScheduleJobTests) + "/" + nameof(RestoreFromQuerier);
            var cronJobScheduled = new CronJobScheduled(new(), new(), "* * * * *", "test", "{}", null, null);
            var eventLog = EventLog.Create(key, 0, DateTime.UtcNow, nameof(CronJobScheduled), cronJobScheduled);
            await CronJobEventStore.CommitEventLog(eventLog);
            CronJob? state = null;
            state = state.Apply(cronJobScheduled, key, eventLog.Timestamp);
            await JobIndexer.Index(state);

            await JobManager.GetCronJob<NoopCommand>(key);
        }
    }
}
