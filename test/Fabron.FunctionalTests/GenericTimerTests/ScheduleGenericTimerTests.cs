using Fabron.Schedulers;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests.GenericTimerTests;

public record TimerData(string Foo);
public class ScheduleGenericTimerTests(DefaultClusterFixture fixture, ITestOutputHelper output) : TestBase(fixture, output)
{
    [Fact]
    public async Task ScheduleAndGet()
    {
        var key = $"{nameof(ScheduleGenericTimerTests)}.{nameof(ScheduleAndGet)}";
        var schedule = DateTimeOffset.UtcNow.AddMonths(1);
        await Client.ScheduleGenericTimer(
            key,
            "Bar",
            schedule
        );

        var timer = await Client.GetGenericTimer(key);

        Assert.NotNull(timer);
        Assert.Equal("Bar", timer!.Data);

        var row = await GetReminderRow<IGenericScheduler>(key);
        row.Should().NotBeNull();
        row!.StartAt.Should().BeCloseTo(schedule.DateTime, TimeSpan.FromSeconds(10));
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
