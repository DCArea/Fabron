﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;

using Fabron.Grains;
using Fabron.Grains.TransientJob;
using Fabron.Mando;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

using Orleans.Runtime;
using Orleans.TestKit;
using Orleans.TestKit.Reminders;

using Xunit;

namespace Fabron.Test.Grains.TransientJob
{

    public class TransientGrainTests : TestKitBase
    {
        [Fact]
        public async Task Create()
        {
            await CreateGrain();

            TransientJobState state = MockState.Object.State;
            Assert.Equal(Command.Name, state.Command.Name);
            Assert.Equal(Command.Data, state.Command.Data);
            Assert.Null(state.ScheduledAt);

            Silo.ReminderRegistry.Mock
                .Verify(m => m.RegisterOrUpdateReminder("Check", TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2)));
        }


        [Fact]
        public async Task Create_10sDelay()
        {
            (string _, DateTime? scheduledAt) = await CreateGrain(TimeSpan.FromSeconds(8));

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
            (string _, DateTime? scheduledAt) = await CreateGrain(TimeSpan.FromSeconds(15));

            TransientJobState state = MockState.Object.State;
            Assert.Equal(Command.Name, state.Command.Name);
            Assert.Equal(Command.Data, state.Command.Data);
            Assert.Equal(scheduledAt, state.ScheduledAt);

            Silo.TimerRegistry.Mock.Verify(t => t.RegisterTimer(TestGrain, It.IsAny<Func<object, Task>>(), null, It.Is<TimeSpan>(ts => ts.Seconds == 14), TimeSpan.MaxValue));

            TestReminder? reminder = (TestReminder)await Silo.ReminderRegistry.GetReminder("Check");
            Assert.NotNull(reminder);
            Assert.Equal(TimeSpan.FromMinutes(2), reminder.DueTime);
            Assert.Equal(TimeSpan.FromMinutes(2), reminder.Period);
        }

        [Fact]
        public async Task Create_1mDelay()
        {
            (string _, DateTime? scheduledAt) = await CreateGrain(TimeSpan.FromMinutes(1.1));

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
            (string _, DateTime? scheduledAt) = await CreateGrain(TimeSpan.FromMinutes(5.1));

            TransientJobState state = MockState.Object.State;
            Assert.Equal(scheduledAt, state.ScheduledAt);

            Silo.TimerRegistry.Mock.VerifyNoOtherCalls();

            TestReminder? reminder = (TestReminder)await Silo.ReminderRegistry.GetReminder("Check");
            Assert.NotNull(reminder);
            reminder.DueTime.Should().BeCloseTo(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(0.1));
            reminder.Period.Should().Equals(TimeSpan.FromMinutes(2));
        }


        public TransientGrainTests()
        {
            MockState = new Mock<IPersistentState<TransientJobState>>();
            MockState.SetupProperty(o => o.State, new TransientJobState());
            MockState.SetupGet(o => o.RecordExists).Returns(false);

            MockMapper = new Mock<IAttributeToFactoryMapper<PersistentStateAttribute>>();
            MockMapper.Setup(o => o.GetFactory(It.IsAny<ParameterInfo>(), It.IsAny<PersistentStateAttribute>())).Returns(context => MockState.Object);

            Silo.AddService(MockMapper.Object);

            Silo.AddServiceProbe<ILogger<TransientJobGrain>>();
            Silo.AddServiceProbe<IMediator>();
        }

        public Mock<IPersistentState<TransientJobState>> MockState { get; }
        public Mock<IAttributeToFactoryMapper<PersistentStateAttribute>> MockMapper { get; }
        public JobCommandInfo Command { get; private set; } = new TestCommand(Guid.NewGuid().ToString()).ToRaw();
        public TransientJobGrain TestGrain { get; private set; } = null!;

        private Task<(string jobId, DateTime? scheduledAt)> CreateGrain(TimeSpan? scheduledAfter = null)
            => CreateGrain(Guid.NewGuid().ToString(), scheduledAfter);

        [MemberNotNull(nameof(TestGrain))]
        private async Task<(string jobId, DateTime? scheduledAt)> CreateGrain(string jobId, TimeSpan? scheduledAfter = null)
        {
            TestGrain = null!;
            TestGrain = await Silo.CreateGrainAsync<TransientJobGrain>(jobId)!;
            if (TestGrain is null) { throw new Exception(); }
            DateTime? scheduledAt = null;
            if (scheduledAfter is not null)
            {
                DateTime utcNow = DateTime.UtcNow;
                scheduledAt = utcNow.Add(scheduledAfter.Value);
            }

            await TestGrain.Create(Command, scheduledAt);
            return (jobId, scheduledAt);
        }

    }
}

