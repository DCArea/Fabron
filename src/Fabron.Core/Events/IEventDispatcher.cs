namespace Fabron.Events;

public interface IEventDispatcher
{
    List<IEventRouter> Routers { get; }

    Task DispatchAsync(FabronEventEnvelop envelop);
}

public class EventDispatcher : IEventDispatcher
{
    public EventDispatcher(IEnumerable<IEventRouter> routers) => Routers = routers.ToList();

    public List<IEventRouter> Routers { get; }

    public Task DispatchAsync(FabronEventEnvelop envelop)
    {
        foreach (var router in Routers)
        {
            if (router.Matches(envelop))
            {
                return router.DispatchAsync(envelop);
            }
        }
        return Task.CompletedTask;
    }
}
