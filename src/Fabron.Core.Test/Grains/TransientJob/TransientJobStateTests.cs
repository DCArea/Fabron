// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Cronos;
using FluentAssertions;
using Fabron.Grains;
using Fabron.Test.Grains;
using Xunit;
using Fabron.Grains.TransientJob;

namespace Core.Test.Grains.CronJob
{
    public class TransientJobStateTests
    {
        [Fact]
        public void Schedule20msAgo()
        {
            var now = DateTime.UtcNow;
            var state = new TransientJobState(Command, now.AddMilliseconds(-20));

            Assert.Equal(TimeSpan.Zero, state.DueTime);
        }

        public JobCommandInfo Command { get; private set; } = new TestCommand(Guid.NewGuid().ToString()).ToRaw();
    }
}
