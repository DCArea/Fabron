using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Fabron.TestRunner.Commands;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;

namespace Fabron.TestRunner.Scenarios
{
    public class LabelBasedQuery : ScenarioBase
    {
        public override IHostBuilder ConfigureHost(IHostBuilder builder)
        {
            return base.ConfigureHost(builder);
        }

        public override ISiloBuilder ConfigureSilo(ISiloBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.UseElasticSearch(options =>
                {
                    options.Server = "http://localhost:9200";
                    options.CronJobIndexName = "testrunner-" + nameof(LabelBasedQuery).ToLower() + "cronjobs";
                    options.JobIndexName = "testrunner-" + nameof(LabelBasedQuery).ToLower() + "jobs";
                    options.ConfigureConnectionSettings = settings =>
                    {
                        settings.DisableDirectStreaming();
                    };
                });
            });
            return base.ConfigureSilo(builder);
        }

        public override async Task RunAsync()
        {

            var labels = new Dictionary<string, string>
            {
                {"fabron.io/test-foo", "test-bar" }
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

            IEnumerable<Contracts.CronJob<NoopCommand>> queried = await JobManager.GetCronJobByLabel<NoopCommand>(labels.First().Key, labels.First().Value);
            queried.Should().HaveCount(1);
            var item = queried.First();
            item.Metadata.Labels[labels.First().Key].Should().Be(labels.First().Value);
            Console.WriteLine(item.Metadata.Labels.First().Key);
        }

    }

}
