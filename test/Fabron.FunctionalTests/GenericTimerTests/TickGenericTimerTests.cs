using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests.GenericTimerTests;

public class TickGenericTimerTests : TestBase
{
    public TickGenericTimerTests(DefaultClusterFixture fixture, ITestOutputHelper output) : base(fixture, output)
    { }

    [Fact]
    public async Task TickTimer()
    {
        var key = $"{nameof(TickGenericTimerTests)}.{nameof(TickTimer)}";
        var utcNow = DateTimeOffset.UtcNow;
        await Client.Generic.Schedule(
            key,
            "Bar",
            utcNow.AddDays(10)
        );

        await Client.Generic.Tick(key);

        var fire = Fires.Single(f => f.Source == $"generic.fabron.io/{key}");
        fire.Time.Should().BeCloseTo(utcNow, TimeSpan.FromSeconds(3));
    }
}
