// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Fabron.Grains;
using Fabron.Grains.Job;
using Fabron.Test.Grains;

using Xunit;

namespace Core.Test.Grains.Job
{
    public class JobStateTests
    {
        [Fact]
        public void Schedule20msAgo()
        {
            DateTime now = DateTime.UtcNow;
            JobState state = new()
            {
                Spec = new()
                {
                    Schedule = now.AddMilliseconds(-20),
                    CommandName = Command.Name,
                    CommandData = Command.Data,
                },
                CreatedAt = now,
            };
            Assert.Equal(TimeSpan.Zero, state.DueTime);
        }

        public JobCommandInfo Command { get; private set; } = new TestCommand(Guid.NewGuid().ToString()).ToRaw();
    }
}
