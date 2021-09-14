namespace Fabron.Events
{
    public interface IJobEvent : IFabronEvent
    {
        public static IJobEvent Get(EventLog eventLog)
            => eventLog.Type switch
            {
                nameof(JobScheduled)
                    => eventLog.GetPayload<JobScheduled>(),
                nameof(JobExecutionStarted)
                    => eventLog.GetPayload<JobExecutionStarted>(),
                nameof(JobExecutionSucceed)
                    => eventLog.GetPayload<JobExecutionSucceed>(),
                nameof(JobExecutionFailed)
                    => eventLog.GetPayload<JobExecutionFailed>(),
                nameof(JobDeleted)
                    => eventLog.GetPayload<JobDeleted>(),
                _ => ThrowHelper.ThrowInvalidEventName<IJobEvent>(eventLog.EntityId, eventLog.Version, eventLog.Type)
            };
    };

}
