using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests.PeriodicTimerTests;

public class PeriodicStopTests : TestBase
{
    public PeriodicStopTests(DefaultClusterFixture fixture, ITestOutputHelper output) : base(fixture, output)
    { }

    [Fact]
    public async Task StopTimer()
    {
        var key = $"{nameof(PeriodicStopTests)}.{nameof(StopTimer)}";
        await Client.SchedulePeriodicTimer(
            key,
            "",
            TimeSpan.FromMinutes(1)
        );

        await Client.StopPeriodicTimer(key);

        var timer = await Client.GetPeriodicTimer(key);
        timer!.Status.NextTick.Should().BeNull();

        var entry = PeriodicTimerStore.Select(s => s.GetAsync(key).Result).First(e => e is not null);
        entry.Should().NotBeNull();
        entry!.State.Status.StartedAt.Should().BeNull();
        entry.State.Status.NextTick.Should().BeNull();
    }
}
