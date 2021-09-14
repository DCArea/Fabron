using System;
using System.Collections.Generic;
using Fabron.Models;

namespace Fabron.Events
{
    public interface ICronJobEvent : IFabronEvent
    {
        public static ICronJobEvent Get(EventLog eventLog)
            => eventLog.Type switch
            {
                nameof(CronJobScheduled)  => eventLog.GetPayload<CronJobScheduled>(),
                nameof(CronJobSuspended)  => eventLog.GetPayload<CronJobSuspended>(),
                nameof(CronJobResumed)  => eventLog.GetPayload<CronJobResumed>(),
                nameof(CronJobCompleted)  => eventLog.GetPayload<CronJobCompleted>(),
                nameof(CronJobDeleted)  => eventLog.GetPayload<CronJobDeleted>(),
                _ => ThrowHelper.ThrowInvalidEventName<ICronJobEvent>(eventLog.EntityId, eventLog.Version, eventLog.Type)
            };
    };

    public record CronJobScheduled(
        Dictionary<string, string> Labels,
        Dictionary<string, string> Annotations,
        string Schedule,
        string CommandName,
        string CommandData,
        DateTime? NotBefore,
        DateTime? ExpirationTime) : ICronJobEvent;

    public record CronJobSuspended() : ICronJobEvent;
    public record CronJobResumed() : ICronJobEvent;

    public record CronJobItemsStatusChanged(List<JobItem> Items): ICronJobEvent;
    public record CronJobCompleted(): ICronJobEvent;
    public record CronJobDeleted(): ICronJobEvent;

    public interface IFabronEvent { };

}
