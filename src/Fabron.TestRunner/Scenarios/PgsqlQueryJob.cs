using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Fabron.Providers.PostgreSQL;
using Fabron.TestRunner.Commands;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;

namespace Fabron.TestRunner.Scenarios;

public class PgsqlQueryJob: IScenario
{
    public async Task PlayAsync()
    {
        string conn = Environment.GetEnvironmentVariable("FABRON_POSTGRESQL_CONNSTR")
            ?? throw new Exception("env FABRON_POSTGRESQL_CONNSTR Missing");
        var services = new ServiceCollection()
            .AddLogging()
            .Configure<PostgreSQLOptions>(options =>
            {
                options.ConnectionString = conn;
            })
            .AddSingleton<IFabronQuerier, PostgreSQLQuerier>()
            .BuildServiceProvider();
        var querier = services.GetRequiredService<IFabronQuerier>();

        var result = await querier.FindJobByOwnerAsync<NoopCommand, NoopCommandResult>(
            @namespace: nameof(TestRunner),
            new Models.OwnerReference
            {
                Kind = "CronJob",
                Name = nameof(ScheduleJob)
            },
            20,
            10
        );

        Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }

}
