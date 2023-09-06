using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests.GenericTimerTests;

public class DeleteGenericTimerTests(DefaultClusterFixture fixture, ITestOutputHelper output) : TestBase(fixture, output)
{
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
    }

    [Fact]
    public async Task ScheduleAfterDeletion()
    {
        var key = $"{nameof(DeleteGenericTimerTests)}.{nameof(ScheduleAfterDeletion)}";
        await Client.ScheduleGenericTimer(
            key,
            "",
            DateTimeOffset.UtcNow.AddMonths(1)
        );

        await Client.DeleteGenericTimer(key);
        var timer = await Client.GetGenericTimer(key);
        Assert.Null(timer);

        await Client.ScheduleGenericTimer(
            key,
            "",
            DateTimeOffset.UtcNow.AddMonths(1)
        );

        timer = await Client.GetGenericTimer(key);
        Assert.NotNull(timer);
    }
}
