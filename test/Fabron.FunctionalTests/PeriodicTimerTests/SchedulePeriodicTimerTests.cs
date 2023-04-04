using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests.PeriodicTimerTests;

public record TimerData(string Foo);
public class SchedulePeriodicTimerTests : TestBase
{
    public SchedulePeriodicTimerTests(DefaultClusterFixture fixture, ITestOutputHelper output) : base(fixture, output)
    { }

    [Fact]
    public async Task ScheduleAndGet()
    {
        var key = $"{nameof(SchedulePeriodicTimerTests)}.{nameof(ScheduleAndGet)}";
        await Client.SchedulePeriodicTimer(
            key,
            "Bar",
            TimeSpan.FromMinutes(1)
        );

        var timer = await Client.GetGenericTimer(key);

        Assert.NotNull(timer);
        Assert.Equal("Bar", timer!.Data);
    }

    [Fact]
    public async Task DeleteTimer()
    {
        var key = $"{nameof(SchedulePeriodicTimerTests)}.{nameof(DeleteTimer)}";
        await Client.ScheduleGenericTimer(
            key,
            "",
            DateTimeOffset.UtcNow.AddMonths(1)
        );

        var timer = await Client.GetGenericTimer(key);
        Assert.NotNull(timer);

        await Client.DeleteGenericTimer(key);
        timer = await Client.GetGenericTimer(key);
        Assert.Null(timer);
    }

}
