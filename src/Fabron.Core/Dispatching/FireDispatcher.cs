namespace Fabron.Dispatching;

internal sealed class FireDispatcher(IEnumerable<IFireRouter> routers) : IFireDispatcher
{
    public List<IFireRouter> Routers { get; } = routers.ToList();

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
