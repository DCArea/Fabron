using Fabron.Models;

namespace Fabron.CloudEvents;

public interface IEventRouter
{
    bool Matches(ScheduleMetadata metadata, CloudEventEnvelop envelop);
    Task DispatchAsync(ScheduleMetadata metadata, CloudEventEnvelop envelop);
}
