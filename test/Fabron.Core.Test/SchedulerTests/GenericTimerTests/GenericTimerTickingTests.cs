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

public class GenericTimerTickingTests
{
    private GenericFakes PrepareGrain(DateTimeOffset? schedule = null, [CallerMemberName] string key = "Default")
    {
        var clock = new FakeSystemClock();
        var reminderRegistry = new FakeReminderRegistry();
        var timerRegistry = new FakeTimerRegistry();
        var context = A.Fake<IGrainContext>();
        var runtime = A.Fake<IGrainRuntime>();
        var store = new InMemoryGenericTimerStore();
        A.CallTo(() => context.GrainId)
            .Returns(GrainId.Create(nameof(GenericScheduler), key));
        A.CallTo(() => context.ActivationServices.GetService(typeof(IReminderRegistry))).Returns(reminderRegistry);
        A.CallTo(() => runtime.TimerRegistry).Returns(timerRegistry);

        if (schedule is not null)
        {
            var state = new GenericTimer
            (
                Metadata: new ScheduleMetadata(
                    Key: key,
                    CreationTimestamp: DateTimeOffset.UtcNow,
                    DeletionTimestamp: null,
                    Owner: null,
                    Extensions: new()
                    ),
                Data: JsonSerializer.Serialize(new { data = new { foo = "bar" } }),
                Spec: new GenericTimerSpec
                (
                    Schedule: schedule.Value
                )
            );
            store.SetAsync(state, Guid.NewGuid().ToString()).GetAwaiter().GetResult();
        }

        var grain = new GenericScheduler(
            context,
            runtime,
            A.Fake<ILogger<GenericScheduler>>(),
            Options.Create(new SchedulerOptions { }),
            clock,
            store,
            A.Fake<IFireDispatcher>());
        return new(grain, timerRegistry, reminderRegistry, clock, store);
    }

    [Fact]
    public async Task Schedule_30s()
    {
        var tickTime = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var (scheduler, timerRegistry, reminderRegistry, clock, _) = PrepareGrain();
        clock.UtcNow = tickTime.AddSeconds(-30).AddMilliseconds(100);
        await (scheduler as IGrainBase).OnActivateAsync(default);

        await scheduler.Schedule(
            JsonSerializer.Serialize(new { data = new { foo = "bar" } }),
            new GenericTimerSpec(tickTime),
            null,
            new());

        var reminder = reminderRegistry.Reminders.Single().Value;
        reminder.DueTime.Should().Be(tickTime - clock.UtcNow);
        var state = await scheduler.GetState();
        state!.Status.NextTick.Should().Be(tickTime);
    }

    [Fact]
    public async Task Schedule_120s()
    {
        var tickTime = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var (scheduler, timerRegistry, reminderRegistry, clock, _) = PrepareGrain();
        clock.UtcNow = tickTime.AddSeconds(-120).AddMilliseconds(100);
        await (scheduler as IGrainBase).OnActivateAsync(default);

        await scheduler.Schedule(
            JsonSerializer.Serialize(new { data = new { foo = "bar" } }),
            new GenericTimerSpec(tickTime),
            null,
            new());

        reminderRegistry.Reminders.Single().Value.DueTime.Should().Be(tickTime - clock.UtcNow);
        var state = await scheduler.GetState();
        state!.Status.NextTick.Should().Be(tickTime);
    }

    [Fact]
    public async Task Schedule_60d()
    {
        var tickTime = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var (scheduler, timerRegistry, reminderRegistry, clock, store) = PrepareGrain();
        clock.UtcNow = tickTime.AddDays(-50).AddMilliseconds(100);
        await (scheduler as IGrainBase).OnActivateAsync(default);

        await scheduler.Schedule(
            JsonSerializer.Serialize(new { data = new { foo = "bar" } }),
            new GenericTimerSpec(tickTime),
            null,
            new());

        var reminder = reminderRegistry.Reminders.Single().Value;
        reminder.DueTime.Should().Be(TimeSpan.FromDays(49));
        var entry = await store.GetAsync(scheduler.GetPrimaryKeyString());
        entry!.State.Status.NextTick.Should().Be(tickTime);
    }

    [Fact]
    public async Task Schedule_60d_NextTick()
    {
        var tickTime = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var (scheduler, timerRegistry, reminderRegistry, clock, _) = PrepareGrain();
        clock.UtcNow = tickTime.AddDays(-60).AddMilliseconds(100);
        await (scheduler as IGrainBase).OnActivateAsync(default);
        await scheduler.Schedule(
            JsonSerializer.Serialize(new { data = new { foo = "bar" } }),
            new GenericTimerSpec(tickTime),
            null,
            new());

        var reminderDueTime = clock.UtcNow.Add(reminderRegistry.Reminders.Single().Value.DueTime);
        clock.UtcNow = reminderDueTime.AddMilliseconds(100);

        await FakeReminderRegistry.Fire(scheduler, Names.TickerReminder, new TickStatus(reminderDueTime.UtcDateTime, Timeout.InfiniteTimeSpan, clock.UtcNow.UtcDateTime));

        var nextReminder = reminderRegistry.Reminders.Single().Value;
        nextReminder.DueTime.Should().Be(tickTime - clock.UtcNow);
    }

    [Fact]
    public async Task Schedule_120d_NextTick()
    {
        var tickTime = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var (scheduler, timerRegistry, reminderRegistry, clock, _) = PrepareGrain();
        clock.UtcNow = tickTime.AddDays(-120).AddMilliseconds(100);
        await (scheduler as IGrainBase).OnActivateAsync(default);
        await scheduler.Schedule(
            JsonSerializer.Serialize(new { data = new { foo = "bar" } }),
            new GenericTimerSpec(tickTime),
            null,
            new());
        var reminderDueTime = clock.UtcNow.Add(reminderRegistry.Reminders.Single().Value.DueTime);
        clock.UtcNow = reminderDueTime.AddMilliseconds(100);
        await FakeReminderRegistry.Fire(scheduler, Names.TickerReminder, new TickStatus(reminderDueTime.UtcDateTime, Timeout.InfiniteTimeSpan, clock.UtcNow.UtcDateTime));

        var nextReminder = reminderRegistry.Reminders.Single().Value;
        nextReminder.DueTime.Should().Be(TimeSpan.FromDays(49));
    }

}

internal record GenericFakes(
    GenericScheduler scheduler,
    FakeTimerRegistry timerRegistry,
    FakeReminderRegistry reminderRegistry,
    FakeSystemClock clock,
    IGenericTimerStore store);
