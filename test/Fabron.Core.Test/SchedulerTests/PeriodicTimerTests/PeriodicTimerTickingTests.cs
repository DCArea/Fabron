using System.Runtime.CompilerServices;
using System.Text.Json;
using Fabron.Dispatching;
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
        var context = A.Fake<IGrainContext>();
        var runtime = A.Fake<IGrainRuntime>();
        var store = new InMemoryPeriodicTimerStore();
        A.CallTo(() => context.GrainId)
            .Returns(GrainId.Create(nameof(PeriodicScheduler), key));
        A.CallTo(() => context.ActivationServices.GetService(typeof(IReminderRegistry))).Returns(reminderRegistry);
        A.CallTo(() => runtime.TimerRegistry).Returns(timerRegistry);

        if (schedule is not null)
        {
            var state = new Models.PeriodicTimer
            (
                Metadata: new ScheduleMetadata(
                    Key: key,
                    CreationTimestamp: DateTimeOffset.UtcNow,
                    DeletionTimestamp: null,
                    Owner: null,
                    Extensions: new()
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
            A.Fake<IFireDispatcher>());
        return new(grain, timerRegistry, reminderRegistry, clock, store);
    }

    [Fact]
    public async Task Schedule_10s()
    {
        var (scheduler, timerRegistry, reminderRegistry, clock, store) = PrepareGrain();
        await (scheduler as IGrainBase).OnActivateAsync(default);

        await scheduler.Schedule(
            JsonSerializer.Serialize(new { data = new { foo = "bar" } }),
            new(TimeSpan.FromSeconds(10)),
            null,
            new());

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
        var (scheduler, timerRegistry, reminderRegistry, clock, store) = PrepareGrain();
        await (scheduler as IGrainBase).OnActivateAsync(default);

        await scheduler.Schedule(
            JsonSerializer.Serialize(new { data = new { foo = "bar" } }),
            new(TimeSpan.FromMinutes(1)),
            null,
            new());

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
        var (scheduler, _, reminderRegistry, clock, _) = PrepareGrain();
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
        var (scheduler, _, reminderRegistry, clock, _) = PrepareGrain();
        await (scheduler as IGrainBase).OnActivateAsync(default);
        clock.UtcNow = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var notAfter = clock.UtcNow.AddDays(30).AddSeconds(15);
        await scheduler.Schedule(
            JsonSerializer.Serialize(new { foo = "bar" }),
            new PeriodicTimerSpec(TimeSpan.FromSeconds(10), null, notAfter),
            null,
            null);

        clock.UtcNow = clock.UtcNow.AddDays(30);

        await ((IRemindable)scheduler).ReceiveReminder(Names.TickerReminder, new TickStatus(new DateTimeOffset(2020, 1, 1, 1, 0, 0, TimeSpan.Zero).DateTime, TimeSpan.FromMinutes(2), clock.UtcNow.AddMilliseconds(100).DateTime));

        reminderRegistry.Reminders.Should().HaveCount(0);
    }



}

internal record PeriodicFakes(
    PeriodicScheduler scheduler,
    FakeTimerRegistry timerRegistry,
    FakeReminderRegistry reminderRegistry,
    FakeSystemClock clock,
    IPeriodicTimerStore store);
