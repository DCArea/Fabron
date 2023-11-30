using Orleans.Runtime;
using Orleans.Timers;

namespace Fabron.Core.Test.SchedulerTests
{
    public class FakeReminderRegistry : IReminderRegistry
    {
        public Dictionary<string, FakeGrainReminder> Reminders { get; } = [];

        public static Task Fire(IRemindable remindable, string reminderName, TickStatus tickerStatus) => remindable.ReceiveReminder(reminderName, tickerStatus);

        public Task<IGrainReminder?> GetReminder(GrainId callingGrainId, string reminderName)
        {
            return Reminders.TryGetValue(reminderName, out var reminder)
                ? Task.FromResult<IGrainReminder?>(reminder)
                : Task.FromResult<IGrainReminder?>(null);
        }

        public Task<List<IGrainReminder>> GetReminders(GrainId callingGrainId)
            => Task.FromResult(Reminders.Values.Cast<IGrainReminder>().ToList());

        public Task<IGrainReminder> RegisterOrUpdateReminder(GrainId callingGrainId, string reminderName, TimeSpan dueTime, TimeSpan period)
        {
            var reminder = new FakeGrainReminder(reminderName, dueTime, period);
            Reminders[reminderName] = reminder;
            return Task.FromResult(reminder as IGrainReminder);
        }

        public Task UnregisterReminder(GrainId callingGrainId, IGrainReminder reminder)
        {
            Reminders.Remove(((FakeGrainReminder)reminder).ReminderName);
            return Task.CompletedTask;
        }

    }

    public class FakeGrainReminder(string reminderName, TimeSpan dueTime, TimeSpan period) : IGrainReminder
    {
        public TimeSpan DueTime { get; } = dueTime;

        public TimeSpan Period { get; } = period;

        public string ReminderName { get; } = reminderName;

        public async Task FireFor(IRemindable grain, DateTimeOffset time)
        {
            await grain.ReceiveReminder(ReminderName, new TickStatus(time.UtcDateTime, Period, time.UtcDateTime));
        }
    }
}
