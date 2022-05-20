using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Fabron.Core.Test.Commands;
using Fabron.Grains;
using Fabron.Models;
using Fabron.Store;
using FakeItEasy;
using FluentAssertions.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Runtime;
using Orleans.Timers;
using Xunit;

namespace Fabron.Core.Test.GrainTests;

public class CronJobGrainTests
{
    public IOptions<CronJobOptions> CronJobOptions
        => Options.Create(new CronJobOptions
        {
            UseSynchronousTicker = true
        });

    private Fakes PrepareGrain([CallerMemberName] string name = "Default")
    {
        string key = KeyUtils.BuildCronJobKey(name, nameof(CronJobGrainTests));
        var context = A.Fake<IGrainContext>();
        var runtime = A.Fake<IGrainRuntime>();
        var clock = A.Fake<ISystemClock>();
        A.CallTo(() => context.GrainId)
            .Returns(GrainId.Create(nameof(CronJobGrain), key));
        A.CallTo(() => runtime.ReminderRegistry).Returns(A.Fake<IReminderRegistry>());
        A.CallTo(() => runtime.TimerRegistry).Returns(A.Fake<ITimerRegistry>());

        var grain = new CronJobGrain(
            context,
            runtime,
            A.Fake<ILogger<CronJobGrain>>(),
            clock,
            CronJobOptions,
            new InMemoryCronJobStore());

        return new(grain, context, runtime, clock);
    }

    [Fact]
    public async Task ShouldScheduleNextTick()
    {
        var fakes = PrepareGrain();
        A.CallTo(() => fakes.clock.UtcNow).Returns(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToDateTimeOffset());
        await (fakes.grain as IGrainBase).OnActivateAsync(default);

        await fakes.grain.Schedule(
            "0 0 1 * *",
            new NoopCommand().Spec,
            null,
            null,
            false,
            null,
            null);

        A.CallTo(() => fakes.runtime.ReminderRegistry.RegisterOrUpdateReminder(
            fakes.context.GrainId,
            "Ticker",
            A<TimeSpan>.That.IsEqualTo(TimeSpan.FromDays(31)),
            A<TimeSpan>.That.IsEqualTo(CronJobOptions.Value.TickerInterval)))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ShouldScheduleNextTick_WhenTheresNoSchedulesForNow()
    {
        var fakes = PrepareGrain();
        A.CallTo(() => fakes.clock.UtcNow).Returns(new DateTime(2020, 1, 5, 0, 0, 0, DateTimeKind.Utc).ToDateTimeOffset());
        await (fakes.grain as IGrainBase).OnActivateAsync(default);

        await fakes.grain.Schedule(
            "0 0 1 * *",
            new NoopCommand().Spec,
            null,
            null,
            false,
            null,
            null);

        A.CallTo(() => fakes.runtime.GrainFactory.GetGrain<IJobGrain>(A<string>.Ignored, null)).MustNotHaveHappened();
        A.CallTo(() => fakes.runtime.ReminderRegistry.RegisterOrUpdateReminder(
            fakes.context.GrainId,
            "Ticker",
            A<TimeSpan>.That.IsEqualTo(TimeSpan.FromDays(27)),
            A<TimeSpan>.That.IsEqualTo(CronJobOptions.Value.TickerInterval)))
            .MustHaveHappenedOnceExactly();
    }
}

internal record Fakes(
    CronJobGrain grain,
    IGrainContext context,
    IGrainRuntime runtime,
    ISystemClock clock);
