using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests.GenericTimerTests;

public class SetExtTest : TestBase
{
    public SetExtTest(DefaultClusterFixture fixture, ITestOutputHelper output) : base(fixture, output)
    { }

    [Fact]
    public async Task SetExt()
    {
        var key = $"{nameof(SetExtTest)}.{nameof(SetExt)}";
        await Client.ScheduleGenericTimer(
            key,
            "Bar",
            DateTimeOffset.UtcNow.AddMonths(1),
            extensions: new Dictionary<string, string>()
            {
                {"toUpdate", "aaa" },
                {"toRemove", "xxx" },
            }
        );

        await Client.SetExtForGenericTimer(key, new Dictionary<string, string?>()
        {
                {"toUpdate", "bbb" },
                {"toRemove", null},
                {"toAdd", "yyy" },
        });

        var timer = await Client.GetGenericTimer(key);

        Assert.Equal("bbb", timer!.Metadata.Extensions["toUpdate"]);
        Assert.False(timer!.Metadata.Extensions.ContainsKey("toRemove"));
        Assert.Equal("yyy", timer!.Metadata.Extensions["toAdd"]);
    }
}
