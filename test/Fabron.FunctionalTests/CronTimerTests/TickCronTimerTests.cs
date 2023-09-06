using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests.CronTimerTests;

public class TickCronTimerTests(DefaultClusterFixture fixture, ITestOutputHelper output) : TestBase(fixture, output)
{
    [Fact]
    public async Task TickTimer()
    {
        var key = $"{nameof(TickCronTimerTests)}.{nameof(TickTimer)}";
        var utcNow = DateTimeOffset.UtcNow;
        await Client.Cron.Schedule(
            key,
            "Bar",
            "0 0 * * *",
            utcNow.AddDays(10)
        );

        await Client.Cron.Tick(key);

        var fire = Fires.Single(f => f.Source == $"cron.fabron.io/{key}");
        fire.Time.Should().BeCloseTo(utcNow, TimeSpan.FromSeconds(3));
    }
}
