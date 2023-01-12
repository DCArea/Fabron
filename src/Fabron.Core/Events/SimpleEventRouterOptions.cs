namespace Fabron.Events;

public class SimpleEventRouterOptions
{
    public List<SimpleEventRouter.Route> Routes { get; init; } = new();
}
