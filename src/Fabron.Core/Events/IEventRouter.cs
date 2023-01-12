namespace Fabron.Events;

public interface IEventRouter
{
    bool Matches(FabronEventEnvelop envelop);
    Task DispatchAsync(FabronEventEnvelop envelop);
}
