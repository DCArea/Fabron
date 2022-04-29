// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using Fabron;
// using Fabron.TestRunner.Commands;
// using FluentAssertions;
// using Orleans.Hosting;

// namespace Fabron.TestRunner.Scenarios;

// public class ScheduleCronJobNonUtc : ScenarioBase
// {
//     protected override IEnumerable<KeyValuePair<string, string>> Configs => new Dictionary<string, string>
//     {
//         { "Logging:LogLevel:Default", "Warning" },
//         { "Logging:LogLevel:Fabron", "Debug"}
//     };

//     public override ISiloBuilder ConfigureSilo(ISiloBuilder builder)
//     {
//         builder.Configure<CronJobOptions>(options =>
//         {
//             options.CronFormat = Cronos.CronFormat.IncludeSeconds;
//             options.TimeZone = TimeZoneInfo.Local;
//         });
//         return base.ConfigureSilo(builder);
//     }

//     public override async Task RunAsync()
//     {
//         var labels = new Dictionary<string, string>
//             {
//                 {"foo", "bar" }
//             };

//         var schedule = DateTimeOffset.UtcNow.LocalDateTime.AddSeconds(3);
//         string cronExp = $"{schedule.Second} {schedule.Minute} {schedule.Hour} {schedule.Day} * *";
//         Contracts.CronJob<NoopCommand> job = await JobManager.ScheduleCronJob(
//             GetType().Name + "/" + nameof(ScheduleCronJob),
//             cronExp,
//             new NoopCommand(),
//             null,
//             null,
//             false,
//             labels,
//             null);
//         Grains.ICronJobGrain grain = GetCronJobGrain(job.Metadata.Key);

//         await Task.Delay(TimeSpan.FromSeconds(5));
//     }

// }
