using System;
using Fabron.TestRunner.Scenarios;

if (args.Length > 0)
{
    string scenarioName = args[0];
    IScenario scenario = scenarioName.ToLowerInvariant() switch
    {
        "schedule" => new ScheduleJob(),
        "schedule_cron" => new ScheduleCronJob(),
        "pgsql_schedule_cron" => new PgsqlScheduleCronJob(),
        "pgsql_query" => new PgsqlQueryJob(),
        _ => throw new ArgumentNullException($"Unknown scenario: {scenarioName}")
    };

    await scenario.PlayAsync();
}
else
{
    throw new ArgumentNullException($"Unknown scenario");
}
