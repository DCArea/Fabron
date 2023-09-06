using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests.PeriodicTimerTests;

public class TickPeriodicTimerTests(DefaultClusterFixture fixture, ITestOutputHelper output) : TestBase(fixture, output)
{
    [Fact]
    public async Task TickTimer()
    {
        var key = $"{nameof(TickPeriodicTimerTests)}.{nameof(TickTimer)}";
        var utcNow = DateTimeOffset.UtcNow;
        await Client.Periodic.Schedule(
            key,
            "Bar",
            TimeSpan.FromDays(10),
            utcNow.AddDays(10)
        );

        await Client.Periodic.Tick(key);

        var fire = Fires.Single(f => f.Source == $"periodic.fabron.io/{key}");
        fire.Time.Should().BeCloseTo(utcNow, TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task TickStoppedTimer()
    {
        var key = $"{nameof(TickPeriodicTimerTests)}.{nameof(TickStoppedTimer)}";
        var utcNow = DateTimeOffset.UtcNow;
        await Client.Periodic.Schedule(
            key,
            "Bar",
            TimeSpan.FromDays(10),
            utcNow.AddDays(10)
        );
        await Client.Periodic.Stop(key);

        await Client.Periodic.Tick(key);

        var fire = Fires.Single(f => f.Source == $"periodic.fabron.io/{key}");
        fire.Time.Should().BeCloseTo(utcNow, TimeSpan.FromSeconds(3));
    }
}
