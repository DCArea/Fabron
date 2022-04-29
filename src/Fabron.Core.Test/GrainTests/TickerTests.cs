using System;
using System.Collections.Generic;
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

public class TickerTests
{
    public IOptions<CronJobOptions> CronJobOptions
        => Options.Create(new CronJobOptions
        {
            UseSynchronousTicker = true
        });

    [Fact]
    public async Task ShouldTickImmediately()
    {
        var context = A.Fake<IGrainContext>();
        var runtime = A.Fake<IGrainRuntime>();
        var clock = A.Fake<ISystemClock>();
        A.CallTo(() => context.GrainId)
            .Returns(GrainId.Create(nameof(CronJobGrain), KeyUtils.BuildCronJobKey(nameof(ShouldTickImmediately), nameof(TickerTests))));
        A.CallTo(() => runtime.ReminderRegistry).Returns(A.Fake<IReminderRegistry>());
        A.CallTo(() => runtime.TimerRegistry).Returns(A.Fake<ITimerRegistry>());
        var grain = new CronJobGrain(
            context,
            runtime,
            A.Fake<ILogger<CronJobGrain>>(),
            clock,
            CronJobOptions,
            new InMemoryJobStore());
        A.CallTo(() => clock.UtcNow).Returns(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToDateTimeOffset());
        var jobGrain = A.Fake<IJobGrain>();
        A.CallTo(() => runtime.GrainFactory.GetGrain<IJobGrain>(A<string>.That.StartsWith("/registry/jobs/TickerTests/fabron-cronjob-ShouldTickImmediately-"), null))
            .Returns(jobGrain).Once();
        await (grain as IGrainBase).OnActivateAsync(default);

        await grain.Schedule(
            "0 * * * *",
            new NoopCommand().Spec,
            null,
            null,
            false,
            null,
            null);

        A.CallTo(() => jobGrain.Schedule(
            A<DateTimeOffset?>.Ignored,
            A<CommandSpec>.Ignored,
            A<Dictionary<string, string>?>.Ignored,
            A<Dictionary<string, string>?>.Ignored,
            A<OwnerReference?>.Ignored
        )).MustHaveHappenedOnceExactly();

    }
}
