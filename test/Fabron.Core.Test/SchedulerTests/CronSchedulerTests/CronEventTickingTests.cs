using System.Runtime.CompilerServices;
using System.Text.Json;
using Cronos;
using Fabron.Events;
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

namespace Fabron.Core.Test.SchedulerTests.CronSchedulerTests;

public class CronEventTickingTests
{
    private Fakes PrepareGrain(string? schedule = null, [CallerMemberName] string key = "Default")
    {
        var clock = new FakeSystemClock();
        var reminderRegistry = new FakeReminderRegistry();
        var timerRegistry = new FakeTimerRegistry();
        var context = A.Fake<IGrainContext>();
        var runtime = A.Fake<IGrainRuntime>();
        var store = A.Fake<ICronEventStore>();
        A.CallTo(() => context.GrainId)
            .Returns(GrainId.Create(nameof(CronScheduler), key));
        A.CallTo(() => context.ActivationServices.GetService(typeof(IReminderRegistry))).Returns(reminderRegistry);
        A.CallTo(() => runtime.TimerRegistry).Returns(timerRegistry);

        if (schedule is not null)
        {
            var state = new CronEvent
            {
                Metadata = new ScheduleMetadata
                {
                    Key = key,
                },
                Data = JsonSerializer.Serialize(new { data = new { foo = "bar" } }),
                Spec = new CronEventSpec
                {
                    Schedule = schedule,
                }
            };
            A.CallTo(() => store.GetAsync(key)).Returns(Task.FromResult<StateEntry<CronEvent>?>(new(state, "0")));
        }

        var grain = new CronScheduler(
            context,
            runtime,
            A.Fake<ILogger<CronScheduler>>(),
            Options.Create(new SchedulerOptions { CronFormat = CronFormat.IncludeSeconds }),
            clock,
            store,
            A.Fake<IEventDispatcher>());

        return new(grain, timerRegistry, reminderRegistry, clock, store);
    }

    [Fact]
    public async Task ShouldSetTimersAndNextTick()
    {
        var (scheduler, timerRegistry, reminderRegistry, clock, _) = PrepareGrain("*/20 * * * * *");
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
        var (scheduler, timerRegistry, reminderRegistry, clock, _) = PrepareGrain("0 0 0 * * *");
        await (scheduler as IGrainBase).OnActivateAsync(default);
        var tickTime = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMilliseconds(20);
        await reminderRegistry.RegisterOrUpdateReminder(scheduler.GetGrainId(), Names.TickerReminder, TimeSpan.FromMilliseconds(10), Timeout.InfiniteTimeSpan);
        clock.UtcNow = tickTime.AddMilliseconds(100);
        await FakeReminderRegistry.Fire(scheduler, Names.TickerReminder, new TickStatus(tickTime.UtcDateTime, Timeout.InfiniteTimeSpan, clock.UtcNow.UtcDateTime));
        timerRegistry.Timers.Should().HaveCount(1);
        timerRegistry.Timers[0].DueTime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task ShouldRegisterNextTickOnSchedule()
    {
        var (scheduler, _, reminderRegistry, clock, _) = PrepareGrain();
        await (scheduler as IGrainBase).OnActivateAsync(default);
        clock.UtcNow = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMilliseconds(20);
        await scheduler.Schedule(
            JsonSerializer.Serialize(new { foo = "bar" }),
            new CronEventSpec { Schedule = "0 0 0 * * *" },
            null,
            null);
        reminderRegistry.Reminders.Should().HaveCount(1);
        reminderRegistry.Reminders.Single().Value.DueTime.Should().Be(new DateTimeOffset(2020, 1, 2, 0, 0, 0, TimeSpan.Zero) - clock.UtcNow);
    }


    [Fact]
    public async Task ShouldIgnoreMissedTick()
    {
        var (scheduler, _, reminderRegistry, clock, _) = PrepareGrain();
        await (scheduler as IGrainBase).OnActivateAsync(default);
        clock.UtcNow = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await scheduler.Schedule(
            JsonSerializer.Serialize(new { foo = "bar" }),
            new CronEventSpec { Schedule = "0 0 0 * * *" },
            null,
            null);

        clock.UtcNow = new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero);

        await ((IRemindable)scheduler).ReceiveReminder(Names.TickerReminder, new TickStatus(new DateTimeOffset(2020, 1, 1, 1, 0, 0, TimeSpan.Zero).DateTime, TimeSpan.FromMinutes(2), clock.UtcNow.AddMilliseconds(100).DateTime));

        reminderRegistry.Reminders.Should().HaveCount(1);
        reminderRegistry.Reminders.Single().Value.DueTime.Should().Be(new DateTimeOffset(2021, 1, 2, 0, 0, 0, TimeSpan.Zero) - clock.UtcNow);
    }

}

internal record Fakes(
    CronScheduler scheduler,
    FakeTimerRegistry timerRegistry,
    FakeReminderRegistry reminderRegistry,
    FakeSystemClock clock,
    ICronEventStore store);
