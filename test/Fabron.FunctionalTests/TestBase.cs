using Fabron.Schedulers;
using Fabron.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Orleans.Runtime;
using Orleans.TestingHost;
using Orleans.Timers;
using Xunit;
using Xunit.Abstractions;

namespace Fabron.FunctionalTests;

public record TimerData(string Foo);
public class TestBase : IClassFixture<DefaultClusterFixture>
{
    protected readonly DefaultClusterFixture _fixture;

    public TestBase(DefaultClusterFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _fixture.ClusterServices.GetRequiredService<ILoggerFactory>()
            .AddXUnit(output);
    }

    public IFabronClient Client => _fixture.Client.ServiceProvider.GetRequiredService<IFabronClient>();
    public IReminderRegistry ReminderRegistry => _fixture.ClusterServices.GetRequiredService<IReminderRegistry>();

    public async Task<ReminderEntry?> GetReminderRow<TScheduler>(string key)
        where TScheduler : IGrainWithStringKey
    {
        var grain = _fixture.Client.GetGrain<TScheduler>(key);
        var grainId = grain.GetGrainId();
        var t = _fixture.HostedCluster.Silos
            .Cast<InProcessSiloHandle>()
            .Select(s => s.SiloHost.Services.GetRequiredService<IReminderTable>().ReadRow(grainId, Names.TickerReminder));
        var rows = await Task.WhenAll(t);
        return rows.SingleOrDefault(r => r != null);
    }

    public IEnumerable<InMemoryPeriodicTimerStore> PeriodicTimerStore
        => _fixture.HostedCluster.Silos
        .Cast<InProcessSiloHandle>()
        .Select(s => s.SiloHost.Services.GetRequiredService<IPeriodicTimerStore>())
        .Cast<InMemoryPeriodicTimerStore>();

    internal Mock<ISystemClock> SystemClockMock => Mock.Get(_fixture.Client.ServiceProvider.GetRequiredService<ISystemClock>());

}
