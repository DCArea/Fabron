using Fabron.Dispatching;
using Fabron.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        var table = _fixture.ClusterServices.GetRequiredService<IReminderTable>();
        var row = await table.ReadRow(grainId, Names.TickerReminder);
        return row;
    }

    public IEnumerable<InMemoryPeriodicTimerStore> PeriodicTimerStore
        => _fixture.HostedCluster.Silos
        .Cast<InProcessSiloHandle>()
        .Select(s => s.SiloHost.Services.GetRequiredService<IPeriodicTimerStore>())
        .Cast<InMemoryPeriodicTimerStore>();

    public IEnumerable<FireEnvelop> Fires =>
        FireDispatcher.SelectMany(i => i.Fires);

    public IEnumerable<TestFireDispatcher> FireDispatcher
        => _fixture.HostedCluster.Silos
        .Cast<InProcessSiloHandle>()
        .Select(s => s.SiloHost.Services.GetRequiredService<IFireDispatcher>())
        .Cast<TestFireDispatcher>();

}
