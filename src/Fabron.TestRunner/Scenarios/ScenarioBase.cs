using System;
using Fabron.Grains;
using Microsoft.Extensions.DependencyInjection;
using Orleans;

namespace Fabron.TestRunner.Scenarios
{

    public class ScenarioBase
    {
        private readonly IServiceProvider _sp;

        public ScenarioBase(IServiceProvider sp)
        {
            _sp = sp;
        }
        public IJobManager JobManager => _sp.GetRequiredService<IJobManager>();
        public IClusterClient ClusterClient => _sp.GetRequiredService<IClusterClient>();
        public IGrainFactory GrainFactory => ClusterClient;

        public ICronJobGrain GetCronJobGrain(string id) => ClusterClient.GetGrain<ICronJobGrain>(id);

    }
}
