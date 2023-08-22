namespace Fabron.Dispatching;

internal sealed class FireDispatcher : IFireDispatcher
{
    public FireDispatcher(IEnumerable<IFireRouter> routers) => Routers = routers.ToList();

    public List<IFireRouter> Routers { get; }

    public Task DispatchAsync(FireEnvelop envelop)
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
