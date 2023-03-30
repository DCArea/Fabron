using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests.CronTimerTests;

public record TimerData(string Foo);
public class ScheduleCronTimerTests : TestBase
{
    public ScheduleCronTimerTests(DefaultClusterFixture fixture, ITestOutputHelper output) : base(fixture, output)
    { }

    [Fact]
    public async Task ScheduleAndGet()
    {
        var key = $"{nameof(ScheduleCronTimerTests)}.{nameof(ScheduleAndGet)}";
        await Client.ScheduleCronTimer(
            key,
            "Bar",
            "* * * * *"
        );

        var timer = await Client.GetCronTimer(key);

        Assert.NotNull(timer);
        Assert.Equal("Bar", timer!.Data);
    }
}
