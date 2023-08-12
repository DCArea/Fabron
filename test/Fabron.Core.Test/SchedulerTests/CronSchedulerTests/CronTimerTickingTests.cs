using System.Text.Json;
using Fabron.Models;
using FluentAssertions;
using Orleans.Runtime;
using Xunit;

namespace Fabron.Core.Test.SchedulerTests.CronSchedulerTests;

public class CronTimerTickingTests : CronTimerTestBase
{
    [Fact]
    public async Task ShouldSetTimersAndNextTick()
    {
        var (scheduler, timerRegistry, reminderRegistry, clock, _, _) = PrepareGrain("*/20 * * * * *");
        await (scheduler as IGrainBase).OnActivateAsync(default);
        var tickTime = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        clock.UtcNow = tickTime.AddMilliseconds(100);
        await scheduler.Tick(tickTime);
        timerRegistry.Timers.Count.Should().Be(6);
        timerRegistry.Timers[0].DueTime.Should().Be(TimeSpan.Zero);
        timerRegistry.Timers[1].DueTime.Should().Be(tickTime.AddSeconds(20) - clock.UtcNow);
        timerRegistry.Timers[2].DueTime.Should().Be(tickTime.AddSeconds(40) - clock.UtcNow);
        timerRegistry.Timers[3].DueTime.Should().Be(tickTime.AddSeconds(60) - clock.UtcNow);
        timerRegistry.Timers[4].DueTime.Should().Be(tickTime.AddSeconds(80) - clock.UtcNow);
        timerRegistry.Timers[5].DueTime.Should().Be(tickTime.AddSeconds(100) - clock.UtcNow);
        reminderRegistry.Reminders.Single().Value.DueTime.Should().Be(tickTime.AddSeconds(120) - clock.UtcNow);
    }

    [Fact]
    public async Task ShouldDispatchForCurrentTick()
    {
        var (scheduler, timerRegistry, reminderRegistry, clock, _, _) = PrepareGrain("0 0 0 * * *");
        await (scheduler as IGrainBase).OnActivateAsync(default);
        var tickTime = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMilliseconds(20);
        await reminderRegistry.RegisterOrUpdateReminder(scheduler.GetGrainId(), Names.TickerReminder, TimeSpan.FromMilliseconds(10), Timeout.InfiniteTimeSpan);
        clock.UtcNow = tickTime.AddMilliseconds(100);
        await FakeReminderRegistry.Fire(scheduler, Names.TickerReminder, new TickStatus(tickTime.UtcDateTime, Timeout.InfiniteTimeSpan, clock.UtcNow.UtcDateTime));
        timerRegistry.Timers.Should().HaveCount(1);
        timerRegistry.Timers[0].DueTime.Should().Be(TimeSpan.Zero);
    }


    [Fact]
    public async Task Schedule_June1st()
    {
        // "0 0 0 1 6 *"
        var (scheduler, timerRegistry, reminderRegistry, clock, store, _) = PrepareGrain();
        clock.UtcNow = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await (scheduler as IGrainBase).OnActivateAsync(default);
        await scheduler.Schedule(
            JsonSerializer.Serialize(new { data = new { foo = "bar" } }),
            new CronTimerSpec("0 0 0 1 6 *"),
            null,
            new());

        var reminder = reminderRegistry.Reminders.Single().Value;
        reminder.DueTime.Should().Be(TimeSpan.FromDays(49));
        var entry = await store.GetAsync(scheduler.GetPrimaryKeyString());
        entry!.State.Status.NextTick.Should().Be(new DateTimeOffset(2020, 6, 1, 0, 0, 0, default));
    }

    //[Fact]
    //public async Task Schedule_4AM_ShangHai()
    //{
    //    var (scheduler, timerRegistry, reminderRegistry, clock, store) = PrepareGrain();
    //    clock.UtcNow = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(8)).ToUniversalTime();
    //    await (scheduler as IGrainBase).OnActivateAsync(default);
    //    await scheduler.Schedule(
    //        JsonSerializer.Serialize(new { data = new { foo = "bar" } }),
    //        new CronTimerSpec("0 0 4 * * *"),
    //        null,
    //        new());

    //    var reminder = reminderRegistry.Reminders.Single().Value;
    //    reminder.DueTime.Should().Be(TimeSpan.FromHours(4));
    //    var entry = await store.GetAsync(scheduler.GetPrimaryKeyString());
    //    entry!.State.Status.NextTick.Should().Be(new DateTimeOffset(2020, 1, 1, 4, 0, 0, TimeSpan.FromHours(8)).ToUniversalTime());
    //}

    [Fact]
    public async Task ShouldRegisterNextTickOnSchedule()
    {
        var (scheduler, _, reminderRegistry, clock, _, _) = PrepareGrain();
        await (scheduler as IGrainBase).OnActivateAsync(default);
        clock.UtcNow = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMilliseconds(20);
        await scheduler.Schedule(
            JsonSerializer.Serialize(new { foo = "bar" }),
            new CronTimerSpec(Schedule: "0 0 0 * * *"),
            null,
            null);
        reminderRegistry.Reminders.Should().HaveCount(1);
        reminderRegistry.Reminders.Single().Value.DueTime.Should().Be(new DateTimeOffset(2020, 1, 2, 0, 0, 0, TimeSpan.Zero) - clock.UtcNow);
    }


    [Fact]
    public async Task ShouldIgnoreMissedTick()
    {
        var (scheduler, _, reminderRegistry, clock, _, _) = PrepareGrain();
        await (scheduler as IGrainBase).OnActivateAsync(default);
        clock.UtcNow = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await scheduler.Schedule(
            JsonSerializer.Serialize(new { foo = "bar" }),
            new CronTimerSpec(Schedule: "0 0 0 * * *"),
            null,
            null);

        clock.UtcNow = new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero);

        await ((IRemindable)scheduler).ReceiveReminder(Names.TickerReminder, new TickStatus(new DateTimeOffset(2020, 1, 1, 1, 0, 0, TimeSpan.Zero).DateTime, TimeSpan.FromMinutes(2), clock.UtcNow.AddMilliseconds(100).DateTime));

        reminderRegistry.Reminders.Should().HaveCount(1);
        reminderRegistry.Reminders.Single().Value.DueTime.Should().Be(new DateTimeOffset(2021, 1, 2, 0, 0, 0, TimeSpan.Zero) - clock.UtcNow);
    }

    [Fact]
    public async Task ShouldScheduleAtNotBeforeTime()
    {
        var (scheduler, _, reminderRegistry, clock, _, _) = PrepareGrain();
        await (scheduler as IGrainBase).OnActivateAsync(default);
        clock.UtcNow = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var notBefore = clock.UtcNow.AddDays(30);
        await scheduler.Schedule(
            JsonSerializer.Serialize(new { foo = "bar" }),
            new CronTimerSpec(Schedule: "0 0 0 * * *", notBefore),
            null,
            null);

        reminderRegistry.Reminders.Should().HaveCount(1);
        reminderRegistry.Reminders.Single().Value.DueTime.Should().Be(notBefore - clock.UtcNow);
    }

    [Fact]
    public async Task ShouldNotScheduleLaterThanNotAfterTime()
    {
        var (scheduler, _, reminderRegistry, clock, _, _) = PrepareGrain();
        await (scheduler as IGrainBase).OnActivateAsync(default);
        clock.UtcNow = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var notAfter = clock.UtcNow.AddDays(30).AddSeconds(15);
        await scheduler.Schedule(
            JsonSerializer.Serialize(new { foo = "bar" }),
            new CronTimerSpec(Schedule: "*/10 * * * * *", null, notAfter),
            null,
            null);

        clock.UtcNow = clock.UtcNow.AddDays(30);

        await ((IRemindable)scheduler).ReceiveReminder(Names.TickerReminder, new TickStatus(new DateTimeOffset(2020, 1, 1, 1, 0, 0, TimeSpan.Zero).DateTime, TimeSpan.FromMinutes(2), clock.UtcNow.AddMilliseconds(100).DateTime));

        reminderRegistry.Reminders.Should().HaveCount(0);
    }

    [Fact]
    public async Task ShouldScheduleTillNotAfter()
    {
        var (scheduler, timerRegistry, reminderRegistry, clock, _, dispatcher) = PrepareGrain();
        await (scheduler as IGrainBase).OnActivateAsync(default);
        clock.UtcNow = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await scheduler.Schedule(
            JsonSerializer.Serialize(new { foo = "bar" }),
            new CronTimerSpec(Schedule: "0 1 0 * * *", null, clock.UtcNow.AddMinutes(2)),
            null,
            null);

        timerRegistry.Timers.Count.Should().Be(1);
        clock.UtcNow = clock.UtcNow.AddMinutes(2).AddMilliseconds(100);
        await timerRegistry.Timers[0].Trigger();

        dispatcher.Envelops.Count.Should().Be(1);
    }
}
