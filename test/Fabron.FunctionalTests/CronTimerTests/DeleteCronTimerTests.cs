﻿using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests.CronTimerTests;

public class DeleteCronTimerTests(DefaultClusterFixture fixture, ITestOutputHelper output) : TestBase(fixture, output)
{
    [Fact]
    public async Task DeleteTimer()
    {
        var key = $"{nameof(DeleteCronTimerTests)}.{nameof(DeleteTimer)}";
        await Client.ScheduleCronTimer(
            key,
            "Bar",
            "* * * * *"
        );

        var timer = await Client.GetCronTimer(key);
        Assert.NotNull(timer);

        await Client.DeleteCronTimer(key);

        timer = await Client.GetCronTimer(key);
        Assert.Null(timer);
    }
}
