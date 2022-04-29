// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using Fabron.Providers.PostgreSQL;
// using Fabron.TestRunner.Commands;
// using FluentAssertions;
// using Npgsql.Logging;
// using Orleans.Hosting;

// namespace Fabron.TestRunner.Scenarios
// {

//     public class PgsqlScheduleCronJob : ScenarioBase
//     {
//         public PgsqlScheduleCronJob()
//         {
//             NpgsqlLogManager.Provider = new ConsoleLoggingProvider(NpgsqlLogLevel.Trace);
//             NpgsqlLogManager.IsParameterLoggingEnabled = true;
//         }
//         protected override IEnumerable<KeyValuePair<string, string>> Configs => base.Configs.Concat(new Dictionary<string, string>
//         {
//             // { "Logging:LogLevel:Fabron.Grains.JobGrain", "Information" },
//             // { "Logging:LogLevel:Fabron.Grains.CronJobEventConsumer", "Debug" },
//             { "Logging:LogLevel:Fabron.Grains.JobEventConsumer", "Debug" },
//         });

//         public override ISiloBuilder ConfigureSilo(ISiloBuilder builder)
//         {
//             builder
//             .Configure<CronJobOptions>(options =>
//             {
//                 options.CronFormat = Cronos.CronFormat.IncludeSeconds;
//             })
//             .Configure<JobOptions>(options =>
//             {
//                 options.UseAsynchronousIndexer = false;
//             })
//             .UsePostgreSQLIndexStore()
//             .UsePostgreSQLEventStore(options =>
//             {
//                 options.ConnectionString = "Server=172.31.174.235;Port=5432;Database=fabron_testrunner;User Id=postgres;Password=11223344;Multiplexing=true;Include Error Detail=true";
//             });
//             return base.ConfigureSilo(builder);
//         }
//         public override async Task RunAsync()
//         {
//             var labels = new Dictionary<string, string>
//             {
//                 {"foo", "bar" }
//             };

//             string key = GetType().Name + "/" + nameof(ScheduleCronJob);
//             Contracts.CronJob<NoopCommand> job = await JobManager.ScheduleCronJob<NoopCommand>(
//                 key,
//                 "0/3 * * * * *",
//                 new NoopCommand(),
//                 null,
//                 null,
//                 false,
//                 labels,
//                 null);
//             await Task.Delay(TimeSpan.FromSeconds(10));

//             Grains.ICronJobGrain? grain = GetCronJobGrain(key);

//             var items = await JobManager.GetJobByCron<NoopCommand, NoopCommandResult>(key);
//             items.Should().NotBeEmpty();

//             var querier = JobQuerier;
//             querier.Should().BeOfType<PostgreSQLQuerier>();
//             var jobs = await JobQuerier.GetCronJobByLabel("foo", "bar");
//             jobs.Should().NotBeEmpty();
//         }

//     }
// }
