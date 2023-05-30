using Microsoft.Extensions.Options;

namespace Fabron.Dispatching;

public class SimpleFireRouter : IFireRouter
{
    private readonly SimpleFireRouterOptions _options;

    public SimpleFireRouter(IOptions<SimpleFireRouterOptions> options) => _options = options.Value;

    public Task DispatchAsync(FireEnvelop envelop)
    {
        foreach (var route in _options.Routes)
        {
            if (route.Matches(envelop))
            {
                return route.HandleAsync(envelop);
            }
        }
        return Task.CompletedTask;
    }

    public bool Matches(FireEnvelop envelop)
        => true;

    public class Route
    {
        public Func<FireEnvelop, bool> Matches { get; init; }
            = (envelop) => true;
        public Func<FireEnvelop, Task> HandleAsync { get; init; }
            = (envelop) => Task.CompletedTask;
    }
}
