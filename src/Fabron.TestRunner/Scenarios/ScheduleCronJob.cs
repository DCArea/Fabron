using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron.TestRunner.Commands;
using Orleans.Hosting;

namespace Fabron.TestRunner.Scenarios
{
    public class ScheduleCronJob : ScenarioBase
    {
        protected override FabronServerBuilder ConfigureServer(FabronServerBuilder builder)
        {
            builder.ConfigureOrleans((ctx, sb) =>
            {
                sb.Configure<CronJobOptions>(options =>
                {
                    options.CronFormat = Cronos.CronFormat.IncludeSeconds;
                });
            });
            return builder;
        }

        protected override async Task RunAsync()
        {
            var labels = new Dictionary<string, string>
            {
                {"foo", "bar" }
            };

            Contracts.CronJob<NoopCommand> job = await JobManager.ScheduleCronJob(
                name: nameof(ScheduleJob),
                @namespace: nameof(TestRunner),
                command: new NoopCommand(),
                cronExp: "0/3 * * * * *");

            // await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}
