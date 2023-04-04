using Fabron.Schedulers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests;

public record TimerData(string Foo);
public class TestBase : IClassFixture<DefaultClusterFixture>
{
    private readonly DefaultClusterFixture _fixture;

    public TestBase(DefaultClusterFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _fixture.ClusterServices.GetRequiredService<ILoggerFactory>()
            .AddXUnit(output);
    }

    public IFabronClient Client => _fixture.Client.ServiceProvider.GetRequiredService<IFabronClient>();

    public Mock<ISystemClock> SystemClockMock => Mock.Get(_fixture.Client.ServiceProvider.GetRequiredService<ISystemClock>());

}
