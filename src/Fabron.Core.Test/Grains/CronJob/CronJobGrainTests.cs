// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;

using Fabron.Core.Test.Grains;
using Fabron.Grains;
using Fabron.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Orleans.TestKit;
using Orleans.TestKit.Reminders;
using Xunit;

namespace Fabron.Test.Grains
{

    public class CronJobGrainTests : GrainTestBase<Models.CronJob>
    {
        [Fact]
        public async Task Schedule_Simple()
        {
            DateTime now = DateTime.UtcNow;
            string cronExp = $"{now.AddMinutes(10).Minute} * * * *";
            string? cronJobId = Guid.NewGuid().ToString();
            CronJobGrain? grain = await Silo.CreateGrainAsync<CronJobGrain>(cronJobId);
            await Schedule(grain, cronExp);

            CronJob state = MockState.Object.State;
            Assert.Equal(cronExp, state.Spec.Schedule);
            Assert.Equal(Command.Name, state.Spec.CommandName);
            Assert.Equal(Command.Data, state.Spec.CommandData);

            TestReminder reminder = (TestReminder)await Silo.ReminderRegistry.GetReminder("Ticker");
            reminder.DueTime.Should().BeCloseTo(TimeSpan.FromSeconds(10 * 60 - DateTime.UtcNow.Second), TimeSpan.FromSeconds(5));
            reminder.Period.Should().Be(TimeSpan.FromMinutes(2));
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
                null);

            IGrainReminder? reminder = await Silo.ReminderRegistry.GetReminder("Ticker");
            reminder.Should().BeNull();
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
                null);

            await grain.Suspend();

            CronJob state = MockState.Object.State;
            Assert.True(state.Spec.Suspend);
            IGrainReminder? reminder = await Silo.ReminderRegistry.GetReminder("Ticker");
            reminder.Should().BeNull();
            Assert.Equal(0, Silo.TimerRegistry.NumberOfActiveTimers);
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
                null);

            await grain.Resume();

            CronJob state = MockState.Object.State;
            Assert.False(state.Spec.Suspend);
            TestReminder reminder = (TestReminder)await Silo.ReminderRegistry.GetReminder("Ticker");
            reminder.DueTime.Should().BeCloseTo(TimeSpan.FromSeconds(3 * 60 - DateTime.UtcNow.Second), TimeSpan.FromSeconds(5));
            reminder.Period.Should().Be(TimeSpan.FromMinutes(2));
            Assert.Equal(1, Silo.TimerRegistry.NumberOfActiveTimers);
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
                null);

            await grain.Delete();

            Assert.Null(await grain.GetState());
            Silo.TimerRegistry.NumberOfActiveTimers.Should().Be(0);
            Assert.Null(await Silo.ReminderRegistry.GetReminder("Ticker"));
        }

        protected override void SetupServices() => Silo.AddServiceProbe<ILogger<CronJobGrain>>();

        public (string Name, string Data) Command { get; private set; } = (Guid.NewGuid().ToString(), "{}");

        private async Task<CronJobGrain> Schedule(string cronExp)
        {
            CronJobGrain grain = await Silo.CreateGrainAsync<CronJobGrain>(Guid.NewGuid().ToString());
            await Schedule(grain, cronExp);
            return grain;
        }

        private async Task Schedule(CronJobGrain grain, string cronExp) => await grain.Schedule(cronExp, Command.Name, Command.Data, null, null, false, null);
    }
}

