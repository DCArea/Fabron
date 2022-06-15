using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cronos;
using Fabron.Core.CloudEvents;
using Fabron.Models;
using Fabron.Schedulers;
using Fabron.Store;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Runtime;
using Orleans.Timers;
using Xunit;

namespace Fabron.Core.Test.SchedulerTests.CronEventSchedulerTests;

public class CronEventTickingTests
{
    private Fakes PrepareGrain(string schedule, [CallerMemberName] string key = "Default")
    {
        var state = new CronEvent
        {
            Metadata = new ScheduleMetadata
            {
                Key = key,
            },
            Spec = new CronEventSpec
            {
                Template = JsonSerializer.Serialize(new { data = new { foo = "bar" } }),
                Schedule = schedule,
            }
        };
        var clock = new FakeSystemClock();
        var reminderRegistry = new FakeReminderRegistry();
        var timerRegistry = new FakeTimerRegistry();
        var context = A.Fake<IGrainContext>();
        var runtime = A.Fake<IGrainRuntime>();
        var store = A.Fake<ICronEventStore>();
        A.CallTo(() => context.GrainId)
            .Returns(GrainId.Create(nameof(CronEventScheduler), key));
        A.CallTo(() => runtime.ReminderRegistry).Returns(reminderRegistry);
        A.CallTo(() => runtime.TimerRegistry).Returns(timerRegistry);
        A.CallTo(() => store.GetAsync(key)).Returns(Task.FromResult<(CronEvent? state, string? eTag)>((state, "0")));

        var grain = new CronEventScheduler(
            context,
            runtime,
            A.Fake<ILogger<CronEventScheduler>>(),
            Options.Create(new CronSchedulerOptions { CronFormat = CronFormat.IncludeSeconds }),
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
}

internal record Fakes(
    CronEventScheduler scheduler,
    FakeTimerRegistry timerRegistry,
    FakeReminderRegistry reminderRegistry,
    FakeSystemClock clock,
    ICronEventStore store);
