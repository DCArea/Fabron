using System.Runtime.CompilerServices;
using System.Text.Json;
using Fabron.Models;
using Fabron.Schedulers;
using Fabron.Stores;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Orleans.Timers;
using Xunit;

namespace Fabron.Core.Test.SchedulerTests.GenericTimerTests;

public class PeriodicTimerTickingTests
{
    private PeriodicFakes PrepareGrain(TimeSpan? schedule = null, [CallerMemberName] string key = "Default")
    {
        var clock = new FakeSystemClock();
        var reminderRegistry = new FakeReminderRegistry();
        var timerRegistry = new FakeTimerRegistry();
        var dispatcher = new FakeFireDispatcher();
        var context = A.Fake<IGrainContext>();
        var runtime = A.Fake<IGrainRuntime>();
        var store = new InMemoryPeriodicTimerStore();
        A.CallTo(() => context.GrainId)
            .Returns(GrainId.Create(nameof(PeriodicScheduler), key));
        A.CallTo(() => context.ActivationServices.GetService(typeof(IReminderRegistry))).Returns(reminderRegistry);
        A.CallTo(() => runtime.TimerRegistry).Returns(timerRegistry);

        if (schedule is not null)
        {
            var state = new PeriodicTimer
            (
                Metadata: new ScheduleMetadata(
                    Key: key,
                    CreationTimestamp: DateTimeOffset.UtcNow,
                    DeletionTimestamp: null,
                    Owner: null,
                    Extensions: []
                    ),
                Data: JsonSerializer.Serialize(new { data = new { foo = "bar" } }),
                Spec: new PeriodicTimerSpec
                (
                    schedule.Value,
                    null,
                    null
                )
            );
            store.SetAsync(state, Guid.NewGuid().ToString()).GetAwaiter().GetResult();
        }

        var grain = new PeriodicScheduler(
            context,
            runtime,
            A.Fake<ILogger<PeriodicScheduler>>(),
            Options.Create(new SchedulerOptions { }),
            clock,
            store,
            dispatcher);
        return new(grain, timerRegistry, reminderRegistry, clock, store, dispatcher);
    }

    [Fact]
    public async Task Schedule_10s()
    {
        var (scheduler, timerRegistry, reminderRegistry, clock, store, _) = PrepareGrain();
        await (scheduler as IGrainBase).OnActivateAsync(default);

        await scheduler.Schedule(
            JsonSerializer.Serialize(new { data = new { foo = "bar" } }),
            new(TimeSpan.FromSeconds(10)),
            null,
            []);

        timerRegistry.Timers.Count.Should().Be(6);
        timerRegistry.Timers[0].DueTime.Should().Be(TimeSpan.Zero);
        timerRegistry.Timers[5].DueTime.Should().Be(TimeSpan.FromSeconds(50));

        var reminder = reminderRegistry.Reminders.Single().Value;
        reminder.DueTime.Should().Be(TimeSpan.FromMinutes(1));

        var reminderDueTime = clock.UtcNow.Add(reminder.DueTime);
        clock.UtcNow = reminderDueTime.AddMilliseconds(100);

        await FakeReminderRegistry.Fire(scheduler, Names.TickerReminder, new TickStatus(reminderDueTime.UtcDateTime, Timeout.InfiniteTimeSpan, clock.UtcNow.UtcDateTime));

        timerRegistry.Timers.Count.Should().Be(12);
        timerRegistry.Timers[6].DueTime.Should().Be(TimeSpan.Zero);
        timerRegistry.Timers[11].DueTime.Should().Be(TimeSpan.FromSeconds(50));
    }

    [Fact]
    public async Task Schedule_1m()
    {
        var (scheduler, timerRegistry, reminderRegistry, clock, store, _) = PrepareGrain();
        await (scheduler as IGrainBase).OnActivateAsync(default);

        await scheduler.Schedule(
            JsonSerializer.Serialize(new { data = new { foo = "bar" } }),
            new(TimeSpan.FromMinutes(1)),
            null,
            []);

        timerRegistry.Timers.Count.Should().Be(1);
        timerRegistry.Timers[0].DueTime.Should().Be(TimeSpan.Zero);
        //timerRegistry.Timers[1].DueTime.Should().Be(TimeSpan.FromMinutes(1));

        var reminder = reminderRegistry.Reminders.Single().Value;
        reminder.DueTime.Should().Be(TimeSpan.FromMinutes(1));

        var reminderDueTime = clock.UtcNow.Add(reminder.DueTime);
        clock.UtcNow = reminderDueTime.AddMilliseconds(100);

        await FakeReminderRegistry.Fire(scheduler, Names.TickerReminder, new TickStatus(reminderDueTime.UtcDateTime, Timeout.InfiniteTimeSpan, clock.UtcNow.UtcDateTime));

        timerRegistry.Timers.Count.Should().Be(2);
        timerRegistry.Timers[1].DueTime.Should().Be(TimeSpan.Zero);
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
            new PeriodicTimerSpec(TimeSpan.FromSeconds(10), notBefore),
            null,
            null);

        reminderRegistry.Reminders.Should().HaveCount(1);
        reminderRegistry.Reminders.Single().Value.DueTime.Should().Be(notBefore - clock.UtcNow);
    }

    [Fact]
    public async Task ShouldNotScheduleLaterThanNotAfterTime()
    {
        var (scheduler, _, reminderRegistry, clock, store, _) = PrepareGrain();
        await (scheduler as IGrainBase).OnActivateAsync(default);
        clock.UtcNow = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var notAfter = clock.UtcNow.AddDays(30).AddSeconds(15);
        await scheduler.Schedule(
            JsonSerializer.Serialize(new { foo = "bar" }),
            new PeriodicTimerSpec(TimeSpan.FromSeconds(10), null, notAfter),
            null,
            null);
        var entry = await store.GetAsync(scheduler.GetPrimaryKeyString());
        clock.UtcNow = clock.UtcNow.AddDays(30);
        entry!.State.Status.NextTick = clock.UtcNow;
        await store.SetAsync(entry.State, entry.ETag);
        clock.UtcNow.AddMilliseconds(50);

        await scheduler.LoadStateAsync();
        await ((IRemindable)scheduler).ReceiveReminder(
            Names.TickerReminder,
            new TickStatus(
                DateTime.Parse("2020-01-02T23:59:59.9998105+00:00"),
                TimeSpan.FromMinutes(2),
                DateTime.Parse("2020-01-02T23:59:59.9998105+00:00")
        ));
        reminderRegistry.Reminders.Should().HaveCount(0);
    }


    [Fact(Skip = "TODO: fix")]
    public async Task ShouldIgnoreFurthurDispatchesIfStopped()
    {
        var (scheduler, timerRegistry, _, clock, _, dispatcher) = PrepareGrain();
        await (scheduler as IGrainBase).OnActivateAsync(default);
        clock.UtcNow = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await scheduler.Schedule(
            JsonSerializer.Serialize(new { foo = "bar" }),
            new PeriodicTimerSpec(TimeSpan.FromSeconds(10)),
            null,
            null);

        timerRegistry.Timers.Count.Should().Be(6);
        await timerRegistry.Timers[0].Trigger();
        await timerRegistry.Timers[1].Trigger();

        await scheduler.Stop();
        await timerRegistry.Timers[2].Trigger();
        await timerRegistry.Timers[3].Trigger();
        await timerRegistry.Timers[4].Trigger();
        await timerRegistry.Timers[5].Trigger();

        dispatcher.Envelops.Count.Should().Be(2);
    }

    [Fact]
    public async Task ShouldScheduleTillNotAfter()
    {
        var (scheduler, timerRegistry, _, clock, _, dispatcher) = PrepareGrain();
        await (scheduler as IGrainBase).OnActivateAsync(default);
        clock.UtcNow = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await scheduler.Schedule(
            JsonSerializer.Serialize(new { foo = "bar" }),
            new PeriodicTimerSpec(TimeSpan.FromSeconds(10), null, clock.UtcNow.AddSeconds(120)),
            null,
            null);

        timerRegistry.Timers.Count.Should().Be(6);
        await timerRegistry.Timers[0].Trigger();
        await timerRegistry.Timers[1].Trigger();
        await timerRegistry.Timers[2].Trigger();
        await timerRegistry.Timers[3].Trigger();
        await timerRegistry.Timers[4].Trigger();
        await timerRegistry.Timers[5].Trigger();

        dispatcher.Envelops.Count.Should().Be(6);
    }

    [Fact]
    public async Task ShouldNotMissLateFires()
    {
        var (scheduler, timerRegistry, reminderRegistry, clock, _, dispatcher) = PrepareGrain();
        await (scheduler as IGrainBase).OnActivateAsync(default);
        clock.UtcNow = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await scheduler.Schedule(
            JsonSerializer.Serialize(new { foo = "bar" }),
            new PeriodicTimerSpec(TimeSpan.FromSeconds(5)),
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
        timerRegistry.Timers.Count.Should().Be(3 * 60 / 5);
    }

}

internal record PeriodicFakes(
    PeriodicScheduler scheduler,
    FakeTimerRegistry timerRegistry,
    FakeReminderRegistry reminderRegistry,
    FakeSystemClock clock,
    IPeriodicTimerStore store,
    FakeFireDispatcher dispatcher);
