
using System;
using System.Threading.Tasks;
using Fabron.Grains;
using Fabron.Models;
using Fabron.Stores;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Toolkit.Diagnostics;
using Orleans.Runtime;
using Orleans.TestKit;
using Orleans.TestKit.Reminders;
using Xunit;

namespace Fabron.Test.Grains
{

    public class CronJobGrainTests : TestKitBase
    {
        public CronJobGrainTests()
        {
            Silo.AddServiceProbe<ILogger<CronJobGrain>>();
            Silo.AddService<IOptions<CronJobOptions>>(Options.Create(new CronJobOptions
            {
                UseAsynchronousIndexer = false
            }));
            Silo.AddService<ICronJobEventStore>(new InMemoryCronJobEventStore());
        }

        [Fact]
        public async Task Schedule_Simple()
        {
            DateTime now = DateTime.UtcNow;
            string cronExp = $"{now.AddMinutes(10).Minute} * * * *";
            string? key = Guid.NewGuid().ToString();
            CronJobGrain? grain = await Silo.CreateGrainAsync<CronJobGrain>(key);
            await Schedule(grain, cronExp);

            CronJob? state = await grain.GetState();
            Guard.IsNotNull(state, nameof(state));
            Assert.Equal(cronExp, state.Spec.Schedule);
            Assert.Equal(Command.Name, state.Spec.CommandName);
            Assert.Equal(Command.Data, state.Spec.CommandData);
        }

        [Fact]
        public async Task Schedule_Suspended()
        {
            CronJobGrain? grain = await Silo.CreateGrainAsync<CronJobGrain>(nameof(Schedule_Suspended));

            await grain.Schedule(
                "* * * * *",
                "TestCommand",
                "{}",
                null,
                null,
                true,
                null,
                null);
        }

        [Fact]
        public async Task Suspend()
        {
            CronJobGrain? grain = await Silo.CreateGrainAsync<CronJobGrain>(nameof(Schedule_Suspended));
            await grain.Schedule(
                "* * * * *",
                "TestCommand",
                "{}",
                null,
                null,
                false,
                null,
                null);

            await grain.Suspend();

            CronJob? state = await grain.GetState();
            Guard.IsNotNull(state, nameof(state));
            Assert.True(state.Spec.Suspend);
        }

        [Fact]
        public async Task Resume()
        {
            CronJobGrain? grain = await Silo.CreateGrainAsync<CronJobGrain>(nameof(Schedule_Suspended));
            await grain.Schedule(
                "* * * * *",
                "TestCommand",
                "{}",
                null,
                null,
                true,
                null,
                null);

            await grain.Resume();

            CronJob? state = await grain.GetState();
            Guard.IsNotNull(state, nameof(state));
        }

        [Fact]
        public async Task Delete()
        {
            CronJobGrain? grain = await Silo.CreateGrainAsync<CronJobGrain>(nameof(Schedule_Suspended));
            await grain.Schedule(
                "* * * * *",
                "TestCommand",
                "{}",
                null,
                null,
                true,
                null,
                null);

            await grain.Delete();

            var state = await grain.GetState();
            Assert.True(state!.Status.Deleted);
        }

        public (string Name, string Data) Command { get; private set; } = (Guid.NewGuid().ToString(), "{}");

        private async Task<CronJobGrain> Schedule(string cronExp)
        {
            CronJobGrain grain = await Silo.CreateGrainAsync<CronJobGrain>(Guid.NewGuid().ToString());
            await Schedule(grain, cronExp);
            return grain;
        }

        private async Task Schedule(CronJobGrain grain, string cronExp) => await grain.Schedule(cronExp, Command.Name, Command.Data, null, null, false, null, null);
    }
}

