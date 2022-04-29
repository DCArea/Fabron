using Fabron.Grains;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests
{
    public class TestBase : IClassFixture<DefaultClusterFixture>
    {
        private readonly DefaultClusterFixture _fixture;

        public TestBase(DefaultClusterFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _fixture.ClusterServices.GetRequiredService<ILoggerFactory>()
                .AddXUnit(output);
        }

        public IJobManager JobManager => _fixture.Client.ServiceProvider.GetRequiredService<IJobManager>();
        // public IJobEventStore JobEventStore => _fixture.ClusterServices.GetRequiredService<IJobEventStore>();
        // public ICronJobEventStore CronJobEventStore => _fixture.ClusterServices.GetRequiredService<ICronJobEventStore>();
        // public IJobIndexer JobIndexer => _fixture.ClusterServices.GetRequiredService<IJobIndexer>();
        // public IJobQuerier JobQuerier => _fixture.ClusterServices.GetRequiredService<IJobQuerier>();

        // public ICronJobGrain GetCronJobGrain(string id) => _fixture.Client.GetGrain<ICronJobGrain>(id);

        public IJobGrain GetJobGrain(string id) => _fixture.Client.GetGrain<IJobGrain>(id);

        public Mock<ISystemClock> SystemClockMock => Mock.Get(_fixture.Client.ServiceProvider.GetRequiredService<ISystemClock>());

    }
}
