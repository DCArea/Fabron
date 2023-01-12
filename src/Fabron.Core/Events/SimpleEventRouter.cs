using Microsoft.Extensions.Options;

namespace Fabron.Events;

public class SimpleEventRouter : IEventRouter
{
    private readonly SimpleEventRouterOptions _options;

    public SimpleEventRouter(IOptions<SimpleEventRouterOptions> options) => _options = options.Value;

    public Task DispatchAsync(FabronEventEnvelop envelop)
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

    public bool Matches(FabronEventEnvelop envelop)
        => true;

    public class Route
    {
        public Func<FabronEventEnvelop, bool> Matches { get; init; }
            = (envelop) => true;
        public Func<FabronEventEnvelop, Task> HandleAsync { get; init; }
            = (envelop) => Task.CompletedTask;
    }
}
