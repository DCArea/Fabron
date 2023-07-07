using Fabron.Schedulers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Timers;
using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests.PeriodicTimerTests;

public class PeriodicStopTests : TestBase
{
    public PeriodicStopTests(DefaultClusterFixture fixture, ITestOutputHelper output) : base(fixture, output)
    { }

    [Fact]
    public async Task StopTimer()
    {
        var key = $"{nameof(PeriodicStopTests)}.{nameof(StopTimer)}";
        await Client.SchedulePeriodicTimer(
            key,
            "",
            TimeSpan.FromMinutes(1)
        );

        await Client.StopPeriodicTimer(key);

        var timer = await Client.GetPeriodicTimer(key);
        timer!.Status.NextTick.Should().BeNull();

        var entry = PeriodicTimerStore.Select(s => s.GetAsync(key).Result).First(e => e is not null);
        entry.Should().NotBeNull();
        entry!.State.Status.StartedAt.Should().BeNull();
        entry.State.Status.NextTick.Should().BeNull();

        var reminder = await GetReminderRow<IPeriodicScheduler>(key);
        reminder.Should().BeNull();

        await Client.Periodic.Start(key);
        entry = PeriodicTimerStore.Select(s => s.GetAsync(key).Result).First(e => e is not null);
        entry.Should().NotBeNull();
        entry!.State.Status.StartedAt.Should().NotBeNull();
        entry.State.Status.NextTick.Should().NotBeNull();
        var row = await GetReminderRow<IPeriodicScheduler>(key);
        row.Should().NotBeNull();
    }
}
