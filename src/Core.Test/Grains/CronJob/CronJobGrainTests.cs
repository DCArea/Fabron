using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Orleans.Runtime;
using Orleans.TestKit;
using TGH.Grains.CronJob;
using TGH.Grains.TransientJob;
using Xunit;

namespace TGH.Test.Grains.CronJob
{

    public class CronJobGrainTests : TestKitBase
    {

        [Fact]
        public async Task Create()
        {
            var cronExp = "0 0 * 11 *";

            await CreateGrain(cronExp);

            CronJobState state = MockState.Object.State;
            Assert.Equal(cronExp, state.CronExp);
            Assert.Equal(Command.Name, state.Command.Name);
            Assert.Equal(Command.Data, state.Command.Data);
            Assert.Equal(TGH.Grains.JobStatus.Created, state.Status);
        }


        public CronJobGrainTests()
        {
            MockState = new Mock<IPersistentState<CronJobState>>();
            MockState.SetupProperty(o => o.State, new CronJobState());
            MockState.SetupGet(o => o.RecordExists).Returns(false);

            MockMapper = new Mock<IAttributeToFactoryMapper<PersistentStateAttribute>>();
            MockMapper.Setup(o => o.GetFactory(It.IsAny<ParameterInfo>(), It.IsAny<PersistentStateAttribute>())).Returns(context => MockState.Object);

            Silo.AddService(MockMapper.Object);

            Silo.AddServiceProbe<ILogger<CronJobGrain>>();
        }

        public Mock<IPersistentState<CronJobState>> MockState { get; }
        public Mock<IAttributeToFactoryMapper<PersistentStateAttribute>> MockMapper { get; }
        public TGH.Grains.JobCommandInfo Command { get; private set; } = new TestCommand(Guid.NewGuid().ToString()).ToRaw();
        public CronJobGrain TestGrain { get; private set; } = null!;

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

