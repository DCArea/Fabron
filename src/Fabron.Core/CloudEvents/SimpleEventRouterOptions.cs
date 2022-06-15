using System.Collections.Generic;

namespace Fabron.CloudEvents;

public class SimpleEventRouterOptions
{
    public List<SimpleEventRouter.Route> Routes { get; init; } = new();
}
