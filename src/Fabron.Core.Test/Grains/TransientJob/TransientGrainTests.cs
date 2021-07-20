// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;

using Fabron.Core.Test.Grains;
using Fabron.Grains;
using Fabron.Grains.TransientJob;
using Fabron.Mando;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

using Orleans.TestKit;
using Orleans.TestKit.Reminders;

using Xunit;

namespace Fabron.Test.Grains.TransientJob
{

    public class TransientGrainTests : GrainTestBase<TransientJobState>
    {
        [Fact]
        public async Task Create_Immediately()
        {
            await Schedule();

            TransientJobState state = MockState.Object.State;
            Assert.Equal(Command.Name, state.Command.Name);
            Assert.Equal(Command.Data, state.Command.Data);
            Assert.Null(state.ScheduledAt);

            Silo.TimerRegistry.Mock.VerifyNoOtherCalls();
            Silo.ReminderRegistry.Mock
                .Verify(m => m.RegisterOrUpdateReminder("Check", TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2)));

            await WaitUntil(state => state.Status == JobStatus.RanToCompletion, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task Create_10sDelay()
        {
            (TransientJobGrain _, DateTime? scheduledAt) = await Schedule(TimeSpan.FromSeconds(8));

            TransientJobState state = MockState.Object.State;
            Assert.Equal(Command.Name, state.Command.Name);
            Assert.Equal(Command.Data, state.Command.Data);
            Assert.Equal(scheduledAt, state.ScheduledAt);

            Silo.TimerRegistry.Mock.VerifyNoOtherCalls();
            Silo.ReminderRegistry.Mock
                .Verify(m => m.RegisterOrUpdateReminder("Check", TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2)));
        }

        [Fact]
        public async Task Create_15sDelay()
        {
            TransientJobGrain grain = await Silo.CreateGrainAsync<TransientJobGrain>(Guid.NewGuid().ToString());
            DateTime? scheduledAt = await Schedule(grain, TimeSpan.FromSeconds(15));

            TransientJobState state = MockState.Object.State;
            Assert.Equal(Command.Name, state.Command.Name);
            Assert.Equal(Command.Data, state.Command.Data);
            Assert.Equal(scheduledAt, state.ScheduledAt);

            Silo.TimerRegistry.Mock.Verify(t => t.RegisterTimer(grain, It.IsAny<Func<object, Task>>(), null, It.Is<TimeSpan>(ts => ts.Seconds == 14), TimeSpan.MaxValue));

            TestReminder? reminder = (TestReminder)await Silo.ReminderRegistry.GetReminder("Check");
            Assert.NotNull(reminder);
            Assert.Equal(TimeSpan.FromMinutes(2), reminder.DueTime);
            Assert.Equal(TimeSpan.FromMinutes(2), reminder.Period);
        }

        [Fact]
        public async Task Create_1mDelay()
        {
            (TransientJobGrain _, DateTime? scheduledAt) = await Schedule(TimeSpan.FromMinutes(1.1));

            TransientJobState state = MockState.Object.State;
            Assert.Equal(scheduledAt, state.ScheduledAt);

            Silo.TimerRegistry.Mock.VerifyNoOtherCalls();

            TestReminder? reminder = (TestReminder)await Silo.ReminderRegistry.GetReminder("Check");
            Assert.NotNull(reminder);
            reminder.DueTime.Should().BeCloseTo(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(0.1));
            reminder.Period.Should().Equals(TimeSpan.FromMinutes(2));
        }

        [Fact]
        public async Task Create_5mDelay()
        {
            (TransientJobGrain _, DateTime? scheduledAt) = await Schedule(TimeSpan.FromMinutes(5.1));

            TransientJobState state = MockState.Object.State;
            Assert.Equal(scheduledAt, state.ScheduledAt);

            Silo.TimerRegistry.Mock.VerifyNoOtherCalls();

            TestReminder? reminder = (TestReminder)await Silo.ReminderRegistry.GetReminder("Check");
            Assert.NotNull(reminder);
            reminder.DueTime.Should().BeCloseTo(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(0.1));
            reminder.Period.Should().Equals(TimeSpan.FromMinutes(2));
        }

        protected override void SetupServices()
        {
            Silo.AddServiceProbe<ILogger<TransientJobGrain>>();
            Silo.AddServiceProbe<IMediator>();
        }


        public JobCommandInfo Command { get; private set; } = new TestCommand(Guid.NewGuid().ToString()).ToRaw();

        private async Task<(TransientJobGrain grain, DateTime? scheduledAt)> Schedule(TimeSpan? scheduledAfter = null)
        {
            TransientJobGrain grain = await Silo.CreateGrainAsync<TransientJobGrain>(Guid.NewGuid().ToString());
            return (grain, await Schedule(grain, scheduledAfter));
        }

        private async Task<DateTime?> Schedule(TransientJobGrain grain, TimeSpan? scheduledAfter = null)
        {
            DateTime? scheduledAt = null;
            if (scheduledAfter is not null)
            {
                DateTime utcNow = DateTime.UtcNow;
                scheduledAt = utcNow.Add(scheduledAfter.Value);
            }

            await grain.Create(Command, scheduledAt);
            return scheduledAt;
        }

    }
}

