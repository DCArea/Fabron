using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests.PeriodicTimerTests;

public class DeletePeriodicTimerTests : TestBase
{
    public DeletePeriodicTimerTests(DefaultClusterFixture fixture, ITestOutputHelper output) : base(fixture, output)
    { }

    [Fact]
    public async Task DeleteTimer()
    {
        var key = $"{nameof(DeletePeriodicTimerTests)}.{nameof(DeleteTimer)}";
        await Client.SchedulePeriodicTimer(
            key,
            "",
            TimeSpan.FromMinutes(1)
        );

        var timer = await Client.GetPeriodicTimer(key);
        Assert.NotNull(timer);

        await Client.DeletePeriodicTimer(key);
        timer = await Client.GetPeriodicTimer(key);
        Assert.Null(timer);
    }
}
