using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests.PeriodicTimerTests;

public class SetExtTest(DefaultClusterFixture fixture, ITestOutputHelper output) : TestBase(fixture, output)
{
    [Fact]
    public async Task SetExt()
    {
        var key = $"{nameof(SetExtTest)}.{nameof(SetExt)}";
        await Client.SchedulePeriodicTimer(
            key,
            "Bar",
            TimeSpan.FromMinutes(10),
            extensions: new Dictionary<string, string>()
            {
                {"toUpdate", "aaa" },
                {"toRemove", "xxx" },
            }
        );

        await Client.SetExtForPeriodicTimer(key, new Dictionary<string, string?>()
        {
                {"toUpdate", "bbb" },
                {"toRemove", null},
                {"toAdd", "yyy" },
        });

        var timer = await Client.GetPeriodicTimer(key);

        Assert.Equal("bbb", timer!.Metadata.Extensions["toUpdate"]);
        Assert.False(timer!.Metadata.Extensions.ContainsKey("toRemove"));
        Assert.Equal("yyy", timer!.Metadata.Extensions["toAdd"]);
    }
}
