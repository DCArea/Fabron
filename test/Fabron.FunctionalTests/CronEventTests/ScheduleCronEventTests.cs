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
        var key = $"{nameof(ScheduleCronEventTests)}.{nameof(ScheduleAndGet)}";
        await Client.ScheduleCronEvent(
            key,
            "Bar",
            "* * * * *"
        );

        var scheduledEvent = await Client.GetCronEvent(key);

        Assert.NotNull(scheduledEvent);
        Assert.Equal("Bar", scheduledEvent!.Data);
    }
}
