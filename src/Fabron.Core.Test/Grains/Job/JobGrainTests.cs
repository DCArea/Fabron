// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;

using Fabron.Core.Test.Grains;
using Fabron.Grains;
using Fabron.Mando;
using Fabron.Models;
using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

using Orleans.TestKit;
using Orleans.TestKit.Reminders;

using Xunit;

namespace Fabron.Test.Grains.Job
{

    public class JobGrainTests : GrainTestBase<Models.Job>
    {
        [Fact]
        public async Task Create_Immediately()
        {
            await Schedule();

            Models.Job state = MockState.Object.State;
            Assert.Equal(Command.Name, state.Spec.CommandName);
            Assert.Equal(Command.Data, state.Spec.CommandData);
            Assert.Equal(state.Metadata.CreationTimestamp, state.Spec.Schedule);

            Silo.TimerRegistry.Mock.VerifyNoOtherCalls();
            Silo.ReminderRegistry.Mock
                .Verify(m => m.RegisterOrUpdateReminder("Check", TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2)));

            await WaitUntil(state => state.Status.ExecutionStatus == ExecutionStatus.Succeed, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task Create_10sDelay()
        {
            (JobGrain grain, DateTime? scheduledAt) = await Schedule(TimeSpan.FromSeconds(8));

            Models.Job state = MockState.Object.State;
            Assert.Equal(Command.Name, state.Spec.CommandName);
            Assert.Equal(Command.Data, state.Spec.CommandData);
            Assert.Equal(scheduledAt, state.Spec.Schedule);

            Silo.TimerRegistry.Mock
                .Verify(m => m.RegisterTimer(grain, It.IsAny<Func<object, Task>>(), null, It.IsAny<TimeSpan>(), TimeSpan.FromMilliseconds(-1)));
            Silo.ReminderRegistry.Mock
                .Verify(m => m.RegisterOrUpdateReminder("Check", TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2)));
        }

        [Fact]
        public async Task Create_15sDelay()
        {
            JobGrain grain = await Silo.CreateGrainAsync<JobGrain>(Guid.NewGuid().ToString());
            DateTime? scheduledAt = await Schedule(grain, TimeSpan.FromSeconds(15));

            Models.Job state = MockState.Object.State;
            Assert.Equal(Command.Name, state.Spec.CommandName);
            Assert.Equal(Command.Data, state.Spec.CommandData);
            Assert.Equal(scheduledAt, state.Spec.Schedule);

            Silo.TimerRegistry.Mock.Verify(t => t.RegisterTimer(grain, It.IsAny<Func<object, Task>>(), null, It.Is<TimeSpan>(ts => ts.Seconds == 14), TimeSpan.FromMilliseconds(-1)));

            TestReminder? reminder = (TestReminder)await Silo.ReminderRegistry.GetReminder("Check");
            Assert.NotNull(reminder);
            Assert.Equal(TimeSpan.FromMinutes(2), reminder.DueTime);
            Assert.Equal(TimeSpan.FromMinutes(2), reminder.Period);
        }

        [Fact]
        public async Task Create_1mDelay()
        {
            (JobGrain grain, DateTime? scheduledAt) = await Schedule(TimeSpan.FromMinutes(1));

            Models.Job state = MockState.Object.State;
            Assert.Equal(scheduledAt, state.Spec.Schedule);

            Silo.TimerRegistry.Mock.Verify(t => t.RegisterTimer(grain, It.IsAny<Func<object, Task>>(), null, It.Is<TimeSpan>(ts => ts.TotalSeconds < 60 && ts.TotalSeconds > 50), TimeSpan.FromMilliseconds(-1)));

            TestReminder? reminder = (TestReminder)await Silo.ReminderRegistry.GetReminder("Check");
            Assert.NotNull(reminder);
            reminder.DueTime.Should().BeCloseTo(TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(0.1));
            reminder.Period.Should().Equals(TimeSpan.FromMinutes(2));
        }

        [Fact]
        public async Task Create_5mDelay()
        {
            (JobGrain _, DateTime? scheduledAt) = await Schedule(TimeSpan.FromMinutes(5.1));

            Models.Job state = MockState.Object.State;
            Assert.Equal(scheduledAt, state.Spec.Schedule);

            Silo.TimerRegistry.Mock.VerifyNoOtherCalls();

            TestReminder? reminder = (TestReminder)await Silo.ReminderRegistry.GetReminder("Check");
            Assert.NotNull(reminder);
            reminder.DueTime.Should().BeCloseTo(TimeSpan.FromMinutes(5 - 2), TimeSpan.FromMinutes(0.1));
            reminder.Period.Should().Equals(TimeSpan.FromMinutes(2));
        }

        protected override void SetupServices()
        {
            Silo.AddServiceProbe<ILogger<JobGrain>>();
            Silo.AddServiceProbe<IMediator>();
        }


        public (string Name, string Data) Command { get; private set; } = (Guid.NewGuid().ToString(), "{}");

        private async Task<(JobGrain grain, DateTime? scheduledAt)> Schedule(TimeSpan? scheduledAfter = null)
        {
            JobGrain grain = await Silo.CreateGrainAsync<JobGrain>(Guid.NewGuid().ToString());
            return (grain, await Schedule(grain, scheduledAfter));
        }

        private async Task<DateTime?> Schedule(JobGrain grain, TimeSpan? scheduledAfter = null)
        {
            DateTime? scheduledAt = null;
            if (scheduledAfter is not null)
            {
                DateTime utcNow = DateTime.UtcNow;
                scheduledAt = utcNow.Add(scheduledAfter.Value);
            }

            await grain.Schedule(Command.Name, Command.Data, scheduledAt);
            return scheduledAt;
        }

    }
}

