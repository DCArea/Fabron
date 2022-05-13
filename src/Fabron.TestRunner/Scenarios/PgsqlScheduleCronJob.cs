using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabron.Providers.PostgreSQL;
using Fabron.TestRunner.Commands;
using FluentAssertions;
using Npgsql.Logging;
using Orleans.Hosting;

namespace Fabron.TestRunner.Scenarios;

public class PgsqlScheduleCronJob : ScenarioBase
{
    public PgsqlScheduleCronJob()
    {
        // NpgsqlLogManager.Provider = new ConsoleLoggingProvider(NpgsqlLogLevel.Trace);
        // NpgsqlLogManager.IsParameterLoggingEnabled = true;
    }
    protected override IEnumerable<KeyValuePair<string, string>> Configs => base.Configs.Concat(new Dictionary<string, string>
    {
        // { "Logging:LogLevel:Fabron.Grains.JobGrain", "Information" },
        // { "Logging:LogLevel:Fabron.Grains.CronJobEventConsumer", "Debug" },
        // { "Logging:LogLevel:Fabron.Grains.JobEventConsumer", "Debug" },
    });

    protected override FabronServerBuilder ConfigureServer(FabronServerBuilder builder)
    {
        builder.ConfigureOrleans((ctx, sb) =>
        {
            sb.Configure<CronJobOptions>(options =>
            {
                options.CronFormat = Cronos.CronFormat.IncludeSeconds;
            });
        });
        string conn = Environment.GetEnvironmentVariable("FABRON_POSTGRESQL_CONNSTR")
            ?? throw new Exception("env FABRON_POSTGRESQL_CONNSTR Missing");
        builder.UsePosgreSQLStore(conn);
        return builder;
    }

    protected override async Task RunAsync()
    {
        Contracts.CronJob<NoopCommand> job = await JobManager.ScheduleCronJob(
            name: nameof(ScheduleJob),
            @namespace: nameof(TestRunner),
            command: new NoopCommand(),
            cronExp: "0/3 * * * * *");
    }

}
