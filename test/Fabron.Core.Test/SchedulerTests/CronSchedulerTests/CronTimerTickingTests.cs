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
        var (scheduler, timerRegistry, reminderRegistry, clock, _, _) = PrepareGrain();
        await (scheduler as IGrainBase).OnActivateAsync(default);
        await scheduler.Schedule(
            JsonSerializer.Serialize(new { foo = "bar" }),
            new CronTimerSpec(Schedule: "0 0 2 * * *"),
            null,
            null);

        var tickTime = new DateTimeOffset(2020, 1, 1, 2, 0, 0, TimeSpan.Zero).AddMilliseconds(20);
        clock.UtcNow = tickTime.AddMilliseconds(100);
        await FakeReminderRegistry.Fire(
            scheduler,
            Names.TickerReminder,
            new TickStatus(
                tickTime.UtcDateTime,
                Timeout.InfiniteTimeSpan,
                tickTime.UtcDateTime));
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
    public async Task ShouldNotIgnoreMissedTick()
    {
        var (scheduler, timerRegistry, reminderRegistry, clock, _, _) = PrepareGrain();
        await (scheduler as IGrainBase).OnActivateAsync(default);
        clock.UtcNow = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await scheduler.Schedule(
            JsonSerializer.Serialize(new { foo = "bar" }),
            new CronTimerSpec(Schedule: "0 0 0 2 * *"),
            null,
            null);

        var tickTime = new DateTimeOffset(2020, 1, 2, 0, 0, 0, TimeSpan.Zero);
        clock.UtcNow = tickTime.AddMinutes(5);

        await ((IRemindable)scheduler).ReceiveReminder(Names.TickerReminder, new TickStatus(tickTime.UtcDateTime, TimeSpan.FromMinutes(2), tickTime.UtcDateTime));

        timerRegistry.Timers.Should().HaveCount(1);
        reminderRegistry.Reminders.Should().HaveCount(1);
        reminderRegistry.Reminders.Single().Value.DueTime.Should().Be(new DateTimeOffset(2020, 2, 2, 0, 0, 0, TimeSpan.Zero) - clock.UtcNow);
    }

    [Fact]
    public async Task ShouldScheduleAtNotBeforeTime()
    {
        var (scheduler, _, reminderRegistry, clock, _, _) = PrepareGrain();
        await (scheduler as IGrainBase).OnActivateAsync(default);
        clock.UtcNow = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var notBefore = clock.UtcNow.AddDays(30);
        clock.UtcNow = clock.UtcNow.AddMilliseconds(100);
        await scheduler.Schedule(
            JsonSerializer.Serialize(new { foo = "bar" }),
            new CronTimerSpec(Schedule: "0 0/3 * * * *", notBefore),
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
            new CronTimerSpec(Schedule: "*/10 * * * * *", clock.UtcNow.AddDays(30), notAfter),
            null,
            null);


        clock.UtcNow = clock.UtcNow.AddDays(30).AddMilliseconds(100);

        await ((IRemindable)scheduler).ReceiveReminder(Names.TickerReminder, new TickStatus(new DateTimeOffset(2020, 1, 1, 1, 0, 0, TimeSpan.Zero).DateTime, TimeSpan.FromMinutes(2), clock.UtcNow.AddMilliseconds(100).DateTime));

        reminderRegistry.Reminders.Should().HaveCount(0);
    }

    [Fact]
    public async Task ShouldNotDispatchDuplicatedTicks()
    {
        var (scheduler, timerRegistry, reminderRegistry, clock, _, dispatcher) = PrepareGrain();
        await (scheduler as IGrainBase).OnActivateAsync(default);
        clock.UtcNow = new DateTimeOffset(2020, 1, 1, 0, 0, 1, TimeSpan.Zero);
        await scheduler.Schedule(
            JsonSerializer.Serialize(new { foo = "bar" }),
            new CronTimerSpec(Schedule: "0 */3 * * * *"),
            null,
            null);

        clock.UtcNow = DateTimeOffset.Parse("2020-01-01T00:03:00.0081234+00:00");
        await ((IRemindable)scheduler).ReceiveReminder(
            Names.TickerReminder,
            new TickStatus(
                DateTime.Parse("2020-01-01T00:02:59.9998105+00:00"),
                TimeSpan.FromMinutes(2),
                DateTime.Parse("2020-01-01T00:02:59.9998105+00:00")
            ));
        timerRegistry.Timers.Count.Should().Be(1);
    }

    [Fact]
    public async Task ShouldScheduleAtFirstTickWhenNotBeforeSet()
    {
        var (scheduler, timerRegistry, reminderRegistry, clock, _, dispatcher) = PrepareGrain();
        await (scheduler as IGrainBase).OnActivateAsync(default);
        clock.UtcNow = DateTimeOffset.Parse("2020-01-01T00:00:00.000+00:00");
        var notBefore = clock.UtcNow.AddDays(29).AddHours(10);
        clock.UtcNow = clock.UtcNow.AddMilliseconds(100);
        await scheduler.Schedule(
            JsonSerializer.Serialize(new { foo = "bar" }),
            new CronTimerSpec(Schedule: "0 0 0 * * *", notBefore),
            null,
            null);

        var tickReminder = reminderRegistry.Reminders.Single().Value;
        tickReminder.DueTime.Should().Be(DateTimeOffset.Parse("2020-01-31T00:00:00.000+00:00") - clock.UtcNow);

        clock.UtcNow = DateTimeOffset.Parse("2020-01-31T00:00:00.000+00:00").AddMilliseconds(100);
        await tickReminder.FireFor(scheduler, DateTimeOffset.Parse("2020-01-31T00:00:00.000+00:00"));
        timerRegistry.Timers.Count.Should().Be(1);
    }

    [Fact]
    public async Task ShouldStartNowIfNotBeforeSetEarlierThanNow()
    {
        var (scheduler, timerRegistry, reminderRegistry, clock, _, dispatcher) = PrepareGrain();
        await (scheduler as IGrainBase).OnActivateAsync(default);
        clock.UtcNow = DateTimeOffset.Parse("2020-01-10T12:00:00.000+00:00");
        var notBefore = clock.UtcNow.AddDays(-3);
        clock.UtcNow = clock.UtcNow.AddMilliseconds(100);
        await scheduler.Schedule(
            JsonSerializer.Serialize(new { foo = "bar" }),
            new CronTimerSpec(Schedule: "0 0 0 * * *", notBefore),
            null,
            null);

        var tickReminder = reminderRegistry.Reminders.Single().Value;
        tickReminder.DueTime.Should().Be(DateTimeOffset.Parse("2020-01-11T00:00:00.000+00:00") - clock.UtcNow);
    }

    [Fact]
    public async Task ShouldNotSkipFireAtNotBeforeTime()
    {
        var (scheduler, timerRegistry, reminderRegistry, clock, _, dispatcher) = PrepareGrain();
        await (scheduler as IGrainBase).OnActivateAsync(default);
        clock.UtcNow = DateTimeOffset.Parse("2020-01-10T12:00:00.000+00:00");
        var notBefore = clock.UtcNow.AddDays(3);
        clock.UtcNow = clock.UtcNow.AddMilliseconds(100);
        await scheduler.Schedule(
            JsonSerializer.Serialize(new { foo = "bar" }),
            new CronTimerSpec(Schedule: "0 0/3 * * * *", notBefore),
            null,
            null);

        var tickReminder = reminderRegistry.Reminders.Single().Value;
        tickReminder.DueTime.Should().Be(notBefore - clock.UtcNow);


        clock.UtcNow = notBefore.AddMilliseconds(100);
        await tickReminder.FireFor(scheduler, notBefore.AddMilliseconds(10));

        var fire = timerRegistry.Timers.Single();
        fire.DueTime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task ShouldNotMissLateFires()
    {
        var (scheduler, timerRegistry, reminderRegistry, clock, _, dispatcher) = PrepareGrain();
        await (scheduler as IGrainBase).OnActivateAsync(default);
        clock.UtcNow = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await scheduler.Schedule(
            JsonSerializer.Serialize(new { foo = "bar" }),
            new CronTimerSpec(Schedule: "* * * * * *"),
            null,
            null);

        clock.UtcNow = DateTimeOffset.Parse("2020-01-01T00:02:00.0081234+00:00");
        await ((IRemindable)scheduler).ReceiveReminder(
            Names.TickerReminder,
            new TickStatus(
                DateTime.Parse("2020-01-01T00:01:59.9998105+00:00"),
                TimeSpan.FromMinutes(2),
                DateTime.Parse("2020-01-01T00:01:59.9998105+00:00")
            ));
        timerRegistry.Timers.Count.Should().Be(4 * 60);
    }
}
