// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Fabron.Grains;
using Fabron.Grains.TransientJob;
using Fabron.Test.Grains;

using Xunit;

namespace Core.Test.Grains.CronJob
{
    public class TransientJobStateTests
    {
        [Fact]
        public void Schedule20msAgo()
        {
            DateTime now = DateTime.UtcNow;
            JobState state = new JobState(Command, now.AddMilliseconds(-20));

            Assert.Equal(TimeSpan.Zero, state.DueTime);
        }

        public JobCommandInfo Command { get; private set; } = new TestCommand(Guid.NewGuid().ToString()).ToRaw();
    }
}
