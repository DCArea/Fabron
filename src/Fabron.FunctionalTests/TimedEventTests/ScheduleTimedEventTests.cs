using Fabron.CloudEvents;
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
                DateTimeOffset.UtcNow.AddMonths(1),
                new CloudEventTemplate<EventData>(
                    new EventData("Bar")
                )
            );

            var timedEvent = await Client.GetTimedEvent<EventData>(key);

            Assert.NotNull(timedEvent);
            Assert.Equal("Bar", timedEvent!.Template.Data.Foo);
        }

    }
}
