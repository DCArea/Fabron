﻿using Fabron.Schedulers;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests.PeriodicTimerTests;

#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
public class PeriodicStopTests(DefaultClusterFixture fixture, ITestOutputHelper output) : TestBase(fixture, output)
{
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
