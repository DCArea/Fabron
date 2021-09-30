using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabron;
using Fabron.Providers.Cassandra;
using Fabron.Providers.PostgreSQL;
using Fabron.TestRunner.Commands;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Orleans.Hosting;
using OrleansCassandraUtils;

namespace Fabron.TestRunner.Scenarios
{

    public class CassandraScheduleCronJob : ScenarioBase
    {
        protected override IEnumerable<KeyValuePair<string, string>> Configs => base.Configs.Concat(new Dictionary<string, string>
        {
            { "Logging:LogLevel:Fabron.Grains.CronJobScheduler", "Information" },
        });

        public override ISiloBuilder ConfigureSilo(ISiloBuilder builder)
        {
            var connectionString = "Contact Point=172.23.26.158;KeySpace=fabron_testrunner;Compression=LZ4";
            builder.Configure<CronJobOptions>(options =>
            {
                options.CronFormat = Cronos.CronFormat.IncludeSeconds;
            })
            .UseCassandraReminderService(options =>
            {
                options.ConnectionString = connectionString;
            })
            .UseCassandraEventStores(options =>
            {
                options.ConnectionString = connectionString;
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
                "0/1 * * * * *",
                new NoopCommand(),
                null,
                null,
                false,
                labels,
                null);
            Grains.ICronJobGrain? grain = GetCronJobGrain(job.Metadata.Key);

            await Task.Delay(TimeSpan.FromSeconds(10));
        }

    }
}
