
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Moq;

using Orleans.Runtime;
using Orleans.TestKit;

namespace Fabron.Core.Test.Grains
{
    public class GrainTestBase<TGrainState> : TestKitBase
        where TGrainState : new()
    {
        public GrainTestBase()
        {
            SetupState();
            SetupServices();
        }

        [MemberNotNull(nameof(MockState))]
        [MemberNotNull(nameof(MockMapper))]
        public void SetupState()
        {
            MockState = new Mock<IPersistentState<TGrainState>>();
            MockState.SetupGet(o => o.RecordExists).Returns(false);
            MockState.SetupGet(o => o.State).Returns(() => State);
            MockState.SetupSet(o => o.State = It.IsAny<TGrainState>()).Callback<TGrainState>(v => State = v);
            MockState.Setup(m => m.WriteStateAsync()).Callback(() => StateWrote.Release());

            MockMapper = new Mock<IAttributeToFactoryMapper<PersistentStateAttribute>>();
            MockMapper.Setup(o => o.GetFactory(It.IsAny<ParameterInfo>(), It.IsAny<PersistentStateAttribute>())).Returns(context => MockState.Object);
            Silo.AddService(MockMapper.Object);
        }

        protected virtual void SetupServices()
        {
        }

        protected Mock<IPersistentState<TGrainState>> MockState { get; private set; }
        protected Mock<IAttributeToFactoryMapper<PersistentStateAttribute>> MockMapper { get; private set; }

        protected SemaphoreSlim StateWrote { get; private set; } = new SemaphoreSlim(1);
        protected TGrainState State { get; private set; } = new();
    }
}
