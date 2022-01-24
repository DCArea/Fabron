using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Fabron.TestRunner.Commands;
using FluentAssertions;

namespace Fabron.TestRunner.Scenarios
{

    public class ScheduleJob : ScenarioBase
    {
        public override async Task RunAsync()
        {
            var labels = new Dictionary<string, string>
            {
                {"foo", "bar" }
            };

            var job = await JobManager.ScheduleJob<NoopCommand, NoopCommandResult>(
                GetType().Name + "/" + nameof(ScheduleJob),
                new NoopCommand(),
                DateTime.Now,
                labels,
                null);

            await Task.Delay(TimeSpan.FromSeconds(5));
            var queried = await JobManager.GetJobByLabel<NoopCommand, NoopCommandResult>("foo", "bar");
            queried.Should().HaveCount(1);

            var values = MetricsHelper.JobExecutionDuration.GetAllLabelValues().ToList();
            Console.WriteLine(JsonSerializer.Serialize(values));
        }

    }
}
