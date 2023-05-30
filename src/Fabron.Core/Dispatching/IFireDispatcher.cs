namespace Fabron.Dispatching;

internal interface IFireDispatcher
{
    List<IFireRouter> Routers { get; }

    Task DispatchAsync(FireEnvelop envelop);
}

internal class FireDispatcher : IFireDispatcher
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
