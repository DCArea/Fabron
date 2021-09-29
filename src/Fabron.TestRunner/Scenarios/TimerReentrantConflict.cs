using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron;
using Fabron.Models;
using Fabron.TestRunner.Commands;
using FluentAssertions;
using Orleans.Hosting;

namespace Fabron.TestRunner.Scenarios
{

    public class TimerReentrantConflict : ScenarioBase
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

            var job = await JobManager.ScheduleCronJob<DelayCommand>(
                GetType().Name + "/" + nameof(ScheduleCronJob),
                "0/20 * * * * *",
                new DelayCommand(100),
                null,
                null,
                false,
                labels,
                null);
            Grains.ICronJobGrain? grain = GetCronJobGrain(job.Metadata.Key);


            await Task.Delay(1000 * 60 * 3);

        }

    }
}
