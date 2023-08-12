using System.Runtime.CompilerServices;
using System.Text.Json;
using Cronos;
using Fabron.Dispatching;
using Fabron.Models;
using Fabron.Schedulers;
using Fabron.Stores;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Orleans.Timers;

namespace Fabron.Core.Test.SchedulerTests.CronSchedulerTests;

public class CronTimerTestBase
{
    internal CronFakes PrepareGrain(string? schedule = null, [CallerMemberName] string key = "Default")
    {
        var clock = new FakeSystemClock();
        var reminderRegistry = new FakeReminderRegistry();
        var timerRegistry = new FakeTimerRegistry();
        var dispatcher = new FakeFireDispatcher();
        var context = A.Fake<IGrainContext>();
        var runtime = A.Fake<IGrainRuntime>();
        var store = new InMemoryCronTimerStore();
        A.CallTo(() => context.GrainId)
            .Returns(GrainId.Create(nameof(CronScheduler), key));
        A.CallTo(() => context.ActivationServices.GetService(typeof(IReminderRegistry))).Returns(reminderRegistry);
        A.CallTo(() => runtime.TimerRegistry).Returns(timerRegistry);

        if (schedule is not null)
        {
            var state = new CronTimer
            (
                Metadata: new ScheduleMetadata(
                    Key: key,
                    CreationTimestamp: DateTimeOffset.UtcNow,
                    DeletionTimestamp: null,
                    Owner: null,
                    Extensions: new()
                    ),
                Data: JsonSerializer.Serialize(new { data = new { foo = "bar" } }),
                Spec: new CronTimerSpec
                (
                    Schedule: schedule,
                    NotBefore: null,
                    NotAfter: null
                )
            );

            store.SetAsync(state, Guid.NewGuid().ToString()).GetAwaiter().GetResult();
        }

        var grain = new CronScheduler(
            context,
            runtime,
            A.Fake<ILogger<CronScheduler>>(),
            Options.Create(new SchedulerOptions { CronFormat = CronFormat.IncludeSeconds }),
            clock,
            store,
            dispatcher);

        return new(grain, timerRegistry, reminderRegistry, clock, store, dispatcher);
    }
}

internal record CronFakes(
    CronScheduler scheduler,
    FakeTimerRegistry timerRegistry,
    FakeReminderRegistry reminderRegistry,
    FakeSystemClock clock,
    ICronTimerStore store,
    FakeFireDispatcher dispatcher);

