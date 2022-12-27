using Fabron.TestRunner.Scenarios;

if (args.Length > 0)
{
    var scenarioName = args[0];
    IScenario scenario = scenarioName.ToLowerInvariant() switch
    {
        _ => throw new ArgumentNullException($"Unknown scenario: {scenarioName}")
    };

    await scenario.PlayAsync();
}
else
{
    throw new ArgumentNullException($"Unknown scenario");
}
