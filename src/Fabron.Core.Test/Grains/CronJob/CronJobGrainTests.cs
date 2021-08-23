// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;

using Fabron.Core.Test.Grains;
using Fabron.Grains;
using Microsoft.Extensions.Logging;

using Orleans.TestKit;

using Xunit;

namespace Fabron.Test.Grains.CronJob
{

    public class CronJobGrainTests : GrainTestBase<Models.CronJob>
    {
        [Fact]
        public async Task Schedule_Simple()
        {
            DateTime now = DateTime.UtcNow;
            string cronExp = $"{now.AddMinutes(10).Minute} * * * *";
            string? cronJobId = Guid.NewGuid().ToString();
            CronJobGrain? cronJobGrain = await Silo.CreateGrainAsync<CronJobGrain>(cronJobId);
            await Schedule(cronJobGrain, cronExp);

            Models.CronJob state = MockState.Object.State;
            Assert.Equal(cronExp, state.Spec.Schedule);
            Assert.Equal(Command.Name, state.Spec.CommandName);
            Assert.Equal(Command.Data, state.Spec.CommandData);
        }


        protected override void SetupServices() => Silo.AddServiceProbe<ILogger<CronJobGrain>>();

        public (string Name, string Data) Command { get; private set; } = (Guid.NewGuid().ToString(), "{}");

        private async Task<CronJobGrain> Schedule(string cronExp)
        {
            CronJobGrain grain = await Silo.CreateGrainAsync<CronJobGrain>(Guid.NewGuid().ToString());
            await Schedule(grain, cronExp);
            return grain;
        }

        private async Task Schedule(CronJobGrain grain, string cronExp) => await grain.Schedule(cronExp, Command.Name, Command.Data, null, null, null);
    }
}

