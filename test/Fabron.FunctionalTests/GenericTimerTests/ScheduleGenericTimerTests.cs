using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests.GenericTimerTests;

public record TimerData(string Foo);
public class ScheduleGenericTimerTests : TestBase
{
    public ScheduleGenericTimerTests(DefaultClusterFixture fixture, ITestOutputHelper output) : base(fixture, output)
    { }

    [Fact]
    public async Task ScheduleAndGet()
    {
        var key = $"{nameof(ScheduleGenericTimerTests)}.{nameof(ScheduleAndGet)}";
        await Client.ScheduleGenericTimer(
            key,
            "Bar",
            DateTimeOffset.UtcNow.AddMonths(1)
        );

        var timer = await Client.GetGenericTimer(key);

        Assert.NotNull(timer);
        Assert.Equal("Bar", timer!.Data);
    }

    [Fact]
    public async Task Schedule50Days()
    {
        var key = $"{nameof(ScheduleGenericTimerTests)}.{nameof(ScheduleAndGet)}";
        await Client.ScheduleGenericTimer(
            key,
            "Bar",
            DateTimeOffset.UtcNow.AddDays(50)
        );

        var timer = await Client.GetGenericTimer(key);

        Assert.NotNull(timer);
        Assert.Equal("Bar", timer!.Data);
    }
}
