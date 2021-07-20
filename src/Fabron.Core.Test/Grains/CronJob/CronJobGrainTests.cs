// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;

using Fabron.Core.Test.Grains;
using Fabron.Grains;
using Fabron.Grains.CronJob;

using Microsoft.Extensions.Logging;

using Orleans.TestKit;

using Xunit;

namespace Fabron.Test.Grains.CronJob
{

    public class CronJobGrainTests : GrainTestBase<CronJobState>
    {
        [Fact]
        public async Task Create()
        {
            string cronExp = "0 0 * 11 *";

            await Schedule(cronExp);

            CronJobState state = MockState.Object.State;
            Assert.Equal(cronExp, state.CronExp);
            Assert.Equal(Command.Name, state.Command.Name);
            Assert.Equal(Command.Data, state.Command.Data);
        }


        [Fact]
        public async Task CreateLongSchedule()
        {
            string cronExp = "0 0 * 11 *";

            await Schedule(cronExp);

            CronJobState state = MockState.Object.State;
            Assert.Equal(cronExp, state.CronExp);
            Assert.Equal(Command.Name, state.Command.Name);
            Assert.Equal(Command.Data, state.Command.Data);
            await WaitUntil(j => j.PendingJobs.Count() == 1, TimeSpan.FromMilliseconds(1000));
        }

        [Fact]
        public async Task CreateShortSchedule()
        {
            string cronExp = "* * * * *";

            await Schedule(cronExp);

            CronJobState state = MockState.Object.State;
            Assert.Equal(cronExp, state.CronExp);
            Assert.Equal(Command.Name, state.Command.Name);
            Assert.Equal(Command.Data, state.Command.Data);
            await WaitUntil(j => j.PendingJobs.Count() == 20, TimeSpan.FromMilliseconds(1000));
        }


        protected override void SetupServices() => Silo.AddServiceProbe<ILogger<CronJobGrain>>();

        public JobCommandInfo Command { get; private set; } = new TestCommand(Guid.NewGuid().ToString()).ToRaw();

        private async Task<CronJobGrain> Schedule(string cronExp)
        {
            CronJobGrain grain = await Silo.CreateGrainAsync<CronJobGrain>(Guid.NewGuid().ToString());
            await Schedule(grain, cronExp);
            return grain;
        }

        private async Task Schedule(CronJobGrain grain, string cronExp) => await grain.Create(cronExp, Command);
    }
}

