using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests.TimedEventTests
{
    public record EventData(string Foo);
    public class ScheduleTimedEventTests : TestBase
    {
        public ScheduleTimedEventTests(DefaultClusterFixture fixture, ITestOutputHelper output) : base(fixture, output)
        { }

        [Fact]
        public async Task ScheduleAndGet()
        {
            var key = $"{nameof(ScheduleTimedEventTests)}.{nameof(ScheduleAndGet)}";
            await Client.ScheduleTimedEvent(
                key,
                "Bar",
                DateTimeOffset.UtcNow.AddMonths(1)
            );

            var timedEvent = await Client.GetTimedEvent(key);

            Assert.NotNull(timedEvent);
            Assert.Equal("Bar", timedEvent!.Data);
        }

    }
}
