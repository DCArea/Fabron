using System;
using System.Threading.Tasks;
using Fabron.CloudEvents;
using Fabron.FunctionalTests.JobTests;
using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests.CronEventTests;

public record EventData(string Foo);
public class ScheduleCronEventTests : TestBase
{
    public ScheduleCronEventTests(DefaultClusterFixture fixture, ITestOutputHelper output) : base(fixture, output)
    { }

    [Fact]
    public async Task ScheduleAndGet()
    {
        string key = $"{nameof(ScheduleCronEventTests)}.{nameof(ScheduleAndGet)}";
        await Client.ScheduleCronEvent(
            key,
            "* * * * *",
            new CloudEventTemplate<EventData>(
                new EventData("Bar")
            )
        );

        var scheduledEvent = await Client.GetCronEvent<EventData>(key);

        Assert.NotNull(scheduledEvent);
        Assert.Equal("Bar", scheduledEvent!.Template.Data.Foo);
    }

}
