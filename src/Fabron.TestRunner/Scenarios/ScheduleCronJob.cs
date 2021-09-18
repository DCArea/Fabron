using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron;
using Fabron.TestRunner.Commands;
using FluentAssertions;

namespace Fabron.TestRunner.Scenarios
{

    public class ScheduleCronJob : ScenarioBase
    {
        public override async Task RunAsync()
        {

            var labels = new Dictionary<string, string>
            {
                {"foo", "bar" }
            };

            Contracts.CronJob<NoopCommand> job = await JobManager.ScheduleCronJob<NoopCommand>(
                this.GetType().Name + "/" + nameof(ScheduleCronJob),
                "* * * * *",
                new NoopCommand(),
                null,
                null,
                false,
                labels,
                null);
            Grains.ICronJobGrain? grain = GetCronJobGrain(job.Metadata.Key);
            await grain.WaitEventsConsumed(10);

            IEnumerable<Contracts.CronJob<NoopCommand>> queried = await JobManager.GetCronJobByLabel<NoopCommand>("foo", "bar");
            queried.Should().HaveCount(1);

        }

    }
}
