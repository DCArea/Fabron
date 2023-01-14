﻿using Orleans.Runtime;
using Orleans.Timers;

namespace Fabron.Core.Test.SchedulerTests
{
    public class FakeReminderRegistry : IReminderRegistry
    {
        public Dictionary<string, FakeGrainReminder> Reminders { get; } = new();

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

    public class FakeGrainReminder : IGrainReminder
    {
        public FakeGrainReminder(string reminderName, TimeSpan dueTime, TimeSpan period)
        {
            ReminderName = reminderName;
            DueTime = dueTime;
            Period = period;
        }

        public TimeSpan DueTime { get; }

        public TimeSpan Period { get; }

        public string ReminderName { get; }
    }
}
