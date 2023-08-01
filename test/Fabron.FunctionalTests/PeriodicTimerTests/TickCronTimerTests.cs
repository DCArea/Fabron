using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests.PeriodicTimerTests;

public class TickPeriodicTimerTests : TestBase
{
    public TickPeriodicTimerTests(DefaultClusterFixture fixture, ITestOutputHelper output) : base(fixture, output)
    { }

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
}
