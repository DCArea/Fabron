// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Cronos;
using FluentAssertions;
using Fabron.Grains;
using Fabron.Grains.CronJob;
using Fabron.Test.Grains;
using Xunit;

namespace Core.Test.Grains.CronJob
{
    public class CronJobStateTests
    {
        public CronJobStateTests()
        {

        }

        [Fact]
        public void ScheduleNextMonth()
        {
            var now = DateTime.UtcNow;
            var cronExp = $"0 0 * {now.AddMonths(1).Month} *";
            var state = new CronJobState(cronExp, Command);

            DateTime toTime = DateTime.UtcNow.AddMinutes(20);
            state.Schedule(toTime);

            var nextSchedule = CronExpression.Parse(cronExp).GetNextOccurrence(now);
            state.NotCreatedJobs.Should().ContainSingle(job => job.ScheduledAt == nextSchedule);
        }

        [Fact]
        public void ScheduleEveryMinute()
        {
            var now = DateTime.UtcNow;
            var cronExp = "* * * * *";
            var state = new CronJobState(cronExp, Command);

            DateTime toTime = DateTime.UtcNow.AddMinutes(20);
            state.Schedule(toTime);

            state.NotCreatedJobs.Should().HaveCount(20);
        }

        public JobCommandInfo Command { get; private set; } = new TestCommand(Guid.NewGuid().ToString()).ToRaw();
    }
}
