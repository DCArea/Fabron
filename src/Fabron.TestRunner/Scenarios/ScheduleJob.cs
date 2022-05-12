using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabron.TestRunner.Commands;
using FluentAssertions;

namespace Fabron.TestRunner.Scenarios;

public class ScheduleJob : ScenarioBase
{
    protected override IEnumerable<KeyValuePair<string, string>> Configs => base.Configs.Concat(new Dictionary<string, string>
        {
            { "Logging:LogLevel:Orleans", "Debug" },
        });

    protected override async Task RunAsync()
    {
        var labels = new Dictionary<string, string>
            {
                {"foo", "bar" }
            };

        var job = await JobManager.ScheduleJob<NoopCommand, NoopCommandResult>(
            name: nameof(TestRunner),
            @namespace: nameof(ScheduleJob),
            command: new NoopCommand(),
            schedule: DateTimeOffset.UtcNow.AddMonths(1),
            labels);

        job.Should().NotBeNull();
        var queried = await JobManager.GetJob<NoopCommand, NoopCommandResult>(nameof(TestRunner), nameof(ScheduleJob));

        queried.Should().NotBeNull();
    }
}
