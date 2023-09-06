using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests.CronTimerTests;

public class SetExtTest(DefaultClusterFixture fixture, ITestOutputHelper output) : TestBase(fixture, output)
{
    [Fact]
    public async Task SetExt()
    {
        var key = $"{nameof(SetExtTest)}.{nameof(SetExt)}";
        await Client.ScheduleCronTimer(
            key,
            "Bar",
            "* * * * *",
            extensions: new Dictionary<string, string>()
            {
                {"toUpdate", "aaa" },
                {"toRemove", "xxx" },
            }
        );

        await Client.SetExtForCronTimer(key, new Dictionary<string, string?>()
        {
                {"toUpdate", "bbb" },
                {"toRemove", null},
                {"toAdd", "yyy" },
        });

        var timer = await Client.GetCronTimer(key);

        Assert.Equal("bbb", timer!.Metadata.Extensions["toUpdate"]);
        Assert.False(timer!.Metadata.Extensions.ContainsKey("toRemove"));
        Assert.Equal("yyy", timer!.Metadata.Extensions["toAdd"]);
    }
}
