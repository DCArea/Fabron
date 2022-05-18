using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Hosting;

namespace Fabron.TestRunner.Scenarios;

public interface IScenario
{
    Task PlayAsync();
}

public abstract class ScenarioBase: IScenario
{
    private IHost _host = default!;
    private IServiceProvider ServiceProvider => _host.Services;
    protected ILogger Logger => ServiceProvider.GetRequiredService<ILogger<ScenarioBase>>();
    protected IJobManager JobManager => ServiceProvider.GetRequiredService<IJobManager>();
    // public IJobQuerier JobQuerier => ServiceProvider.GetRequiredService<IJobQuerier>();
    protected IClusterClient ClusterClient => ServiceProvider.GetRequiredService<IClusterClient>();
    protected IGrainFactory GrainFactory => ClusterClient;

    // public ICronJobGrain GetCronJobGrain(string id) => ClusterClient.GetGrain<ICronJobGrain>(id);

    protected virtual IHostBuilder ConfigureHost(IHostBuilder builder)
    {
        return builder;
    }

    protected virtual FabronServerBuilder ConfigureServer(FabronServerBuilder builder)
    {
        return builder;
    }

    protected virtual FabronClientBuilder ConfigureClient(FabronClientBuilder builder)
    {
        return builder;
    }

    protected virtual IEnumerable<KeyValuePair<string, string>> Configs => new Dictionary<string, string>
        {
            { "Logging:LogLevel:Default", "Warning" },
            { "Logging:LogLevel:Orleans", "Warning" },
            { "Logging:LogLevel:Fabron", "Information"}
        };

    protected virtual IHost CreateHost()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureHostConfiguration(config =>
            {
                config.AddInMemoryCollection(Configs);
            })
            .ConfigureServices(services =>
            {
                services.AddOpenTelemetryTracing(options => options
                    .AddSource("orleans.runtime.graincall")
                // .AddConsoleExporter()
                );
                services.AddFabronClient();
            });

        builder = ConfigureHost(builder);
        var server = builder.UseFabron()
            .UseLocalhostClustering()
            .UseInMemory();
        ConfigureServer(server);

        builder.UseConsoleLifetime();
        return builder.Build();
    }

    protected abstract Task RunAsync();

    public async Task PlayAsync()
    {
        _host = CreateHost();
        await _host.StartAsync();
        await RunAsync();
        await _host.WaitForShutdownAsync();
        // await _host.StopAsync().WaitAsync(TimeSpan.FromSeconds(10));
        Console.WriteLine("FINISHED");
    }
}
