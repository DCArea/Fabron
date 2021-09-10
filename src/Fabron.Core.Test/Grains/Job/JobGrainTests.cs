
using System;
using System.Threading.Tasks;
using Fabron.Grains;
using Fabron.Mando;
using Fabron.Models;
using Fabron.Stores;
using FluentAssertions;

using Microsoft.Extensions.Logging;
using Moq;

using Orleans.TestKit;
using Orleans.TestKit.Reminders;

using Xunit;

namespace Fabron.Test.Grains.Job
{
    public class JobGrainTests : TestKitBase
    {
        public JobGrainTests()
        {
            Silo.AddServiceProbe<ILogger<JobGrain>>();
            Silo.AddServiceProbe<IMediator>();
            Silo.AddService<IJobEventStore>(new InMemoryJobEventStore());
        }

        [Fact]
        public async Task Create_Immediately()
        {
            (JobGrain grain, DateTime? _) = await Schedule();

            Models.Job? state = await grain.GetState();
            Assert.Equal(Command.Name, state!.Spec.CommandName);
            Assert.Equal(Command.Data, state.Spec.CommandData);
            state.Metadata.CreationTimestamp.Should().BeCloseTo(state.Spec.Schedule, TimeSpan.FromSeconds(1));
            //Assert.Equal(state.Metadata.CreationTimestamp, state.Spec.Schedule);

            Silo.TimerRegistry.Mock
                .Verify(m => m.RegisterTimer(
                    grain,
                    It.IsAny<Func<object, Task>>(),
                    null,
                    TimeSpan.Zero,
                    TimeSpan.FromMilliseconds(-1)));
            Silo.ReminderRegistry.Mock
                .Verify(m => m.RegisterOrUpdateReminder("Ticker", TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2)));

            state.Status.ExecutionStatus.Should().Be(ExecutionStatus.Scheduled);
        }

        [Fact]
        public async Task Create_10sDelay()
        {
            (JobGrain grain, DateTime? scheduledAt) = await Schedule(TimeSpan.FromSeconds(10));

            Models.Job? state = await grain.GetState();
            Assert.Equal(Command.Name, state!.Spec.CommandName);
            Assert.Equal(Command.Data, state.Spec.CommandData);
            Assert.Equal(scheduledAt, state.Spec.Schedule);

            Silo.TimerRegistry.Mock
                .Verify(m => m.RegisterTimer(
                    grain,
                    It.IsAny<Func<object, Task>>(),
                    null,
                    It.IsInRange<TimeSpan>(
                        TimeSpan.FromSeconds(6),
                        TimeSpan.FromSeconds(10),
                        Moq.Range.Inclusive),
                    TimeSpan.FromMilliseconds(-1)));
            TestReminder reminder = (TestReminder)await Silo.ReminderRegistry.GetReminder("Ticker");
            reminder.DueTime.Should().BeCloseTo(TimeSpan.FromSeconds(2 * 60 + 10), TimeSpan.FromSeconds(2));
            reminder.Period.Should().Be(TimeSpan.FromMinutes(2));
        }

        [Fact]
        public async Task Create_5mDelay()
        {
            (JobGrain grain, DateTime? scheduledAt) = await Schedule(TimeSpan.FromMinutes(5));

            Models.Job? state = await grain.GetState();
            Assert.Equal(scheduledAt, state!.Spec.Schedule);

            Silo.TimerRegistry.Mock.VerifyNoOtherCalls();

            TestReminder? reminder = (TestReminder)await Silo.ReminderRegistry.GetReminder("Ticker");
            Assert.NotNull(reminder);
            reminder.DueTime.Should().BeCloseTo(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(0.1));
            reminder.Period.Should().Equals(TimeSpan.FromMinutes(2));
        }

        [Fact]
        public async Task Delete()
        {
            (JobGrain grain, DateTime? scheduledAt) = await Schedule(TimeSpan.FromDays(1));

            await grain.Delete();

            Assert.Null(await grain.GetState());
            Silo.TimerRegistry.NumberOfActiveTimers.Should().Be(0);
            Assert.Null(await Silo.ReminderRegistry.GetReminder("Ticker"));
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

