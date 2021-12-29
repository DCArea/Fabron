using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron;
using Fabron.TestRunner.Commands;
using FluentAssertions;
using Orleans.Hosting;

namespace Fabron.TestRunner.Scenarios
{

    public class ScheduleCronJob : ScenarioBase
    {
        public override ISiloBuilder ConfigureSilo(ISiloBuilder builder)
        {
            builder.Configure<CronJobOptions>(options =>
            {
                options.CronFormat = Cronos.CronFormat.IncludeSeconds;
            });
            return base.ConfigureSilo(builder);
        }
        public override async Task RunAsync()
        {

            var labels = new Dictionary<string, string>
            {
                {"foo", "bar" }
            };

            Contracts.CronJob<NoopCommand> job = await JobManager.ScheduleCronJob<NoopCommand>(
                GetType().Name + "/" + nameof(ScheduleCronJob),
                "0/3 * * * * *",
                new NoopCommand(),
                null,
                null,
                false,
                labels,
                null);
            Grains.ICronJobGrain? grain = GetCronJobGrain(job.Metadata.Key);

            await Task.Delay(TimeSpan.FromSeconds(5));
            IEnumerable<Contracts.CronJob<NoopCommand>> queried = await JobManager.GetCronJobByLabel<NoopCommand>("foo", "bar");
            queried.Should().HaveCount(1);

            // await Task.Delay(TimeSpan.FromSeconds(10));
        }

    }
}
