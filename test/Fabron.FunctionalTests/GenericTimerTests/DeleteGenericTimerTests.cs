using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests.GenericTimerTests;

public class DeleteGenericTimerTests : TestBase
{
    public DeleteGenericTimerTests(DefaultClusterFixture fixture, ITestOutputHelper output) : base(fixture, output)
    { }

    [Fact]
    public async Task DeleteTimer()
    {
        var key = $"{nameof(DeleteGenericTimerTests)}.{nameof(DeleteTimer)}";
        await Client.ScheduleGenericTimer(
            key,
            "",
            DateTimeOffset.UtcNow.AddMonths(1)
        );

        var timer = await Client.GetGenericTimer(key);
        Assert.NotNull(timer);

        await Client.DeleteGenericTimer(key);
        timer = await Client.GetGenericTimer(key);
        Assert.Null(timer);
        var ticker = await Client.GetGenericTimerTickerStatus(key);
        Assert.Null(ticker.NextTick);
        Assert.Empty(ticker.RecentDispatches);
    }

}
