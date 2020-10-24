// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Orleans.Runtime;
using Orleans.TestKit;
using TGH.Grains.CronJob;
using Xunit;

namespace TGH.Test.Grains.CronJob
{

    public class CronJobGrainTests : TestKitBase
    {
        private CronJobState _state = new CronJobState();

        [Fact]
        public async Task Create()
        {
            var cronExp = "0 0 * 11 *";

            await CreateGrain(cronExp);

            CronJobState state = MockState.Object.State;
            Assert.Equal(cronExp, state.CronExp);
            Assert.Equal(Command.Name, state.Command.Name);
            Assert.Equal(Command.Data, state.Command.Data);
        }


        [Fact]
        public async Task CreateLongSchedule()
        {
            var cronExp = "0 0 * 11 *";

            await CreateGrain(cronExp);

            CronJobState state = MockState.Object.State;
            Assert.Equal(cronExp, state.CronExp);
            Assert.Equal(Command.Name, state.Command.Name);
            Assert.Equal(Command.Data, state.Command.Data);
            await WhenState(j => j.PendingJobs.Count() == 1, TimeSpan.FromMilliseconds(1000));
        }

        [Fact]
        public async Task CreateShortSchedule()
        {
            var cronExp = "* * * * *";

            await CreateGrain(cronExp);

            CronJobState state = MockState.Object.State;
            Assert.Equal(cronExp, state.CronExp);
            Assert.Equal(Command.Name, state.Command.Name);
            Assert.Equal(Command.Data, state.Command.Data);
            await WhenState(j => j.PendingJobs.Count() == 20, TimeSpan.FromMilliseconds(1000));
        }


        public CronJobGrainTests()
        {
            MockState = new Mock<IPersistentState<CronJobState>>();
            MockState.SetupGet(o => o.RecordExists).Returns(false);
            MockState.SetupGet(o => o.State).Returns(() => State);
            MockState.SetupSet(o => o.State = It.IsAny<CronJobState>()).Callback<CronJobState>(v => State = v);
            MockState.Setup(m => m.WriteStateAsync()).Callback(() => StateWrote.Release());

            MockMapper = new Mock<IAttributeToFactoryMapper<PersistentStateAttribute>>();
            MockMapper.Setup(o => o.GetFactory(It.IsAny<ParameterInfo>(), It.IsAny<PersistentStateAttribute>())).Returns(context => MockState.Object);

            Silo.AddService(MockMapper.Object);

            Silo.AddServiceProbe<ILogger<CronJobGrain>>();
        }

        public Mock<IPersistentState<CronJobState>> MockState { get; }
        public Mock<IAttributeToFactoryMapper<PersistentStateAttribute>> MockMapper { get; }
        public TGH.Grains.JobCommandInfo Command { get; private set; } = new TestCommand(Guid.NewGuid().ToString()).ToRaw();
        public CronJobGrain TestGrain { get; private set; } = null!;

        public SemaphoreSlim StateWrote { get; set; } = new SemaphoreSlim(1);
        public CronJobState State { get => _state; private set => _state = value; }

        public async Task WhenState(Expression<Func<CronJobState, bool>> condition, TimeSpan timeout)
        {
            var con = condition.Compile();
            var token = new CancellationTokenSource(timeout);
            while (!con(State))
            {
                await StateWrote.WaitAsync(token.Token);
            }

        }

        private Task<Guid> CreateGrain(string cronExp)
            => CreateGrain(Guid.NewGuid(), cronExp);

        [MemberNotNull(nameof(TestGrain))]
        private async Task<Guid> CreateGrain(Guid jobId, string cronExp)
        {
            TestGrain = null!;
            TestGrain = await Silo.CreateGrainAsync<CronJobGrain>(jobId)!;
            if (TestGrain is null) { throw new Exception(); }

            await TestGrain.Create(cronExp, Command);
            return jobId;
        }
    }
}

