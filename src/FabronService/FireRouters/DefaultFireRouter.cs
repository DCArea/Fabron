using CommunityToolkit.Diagnostics;
using Fabron.Dispatching;

namespace FabronService.FireRouters;

public class DefaultFireRouter(IHttpDestinationHandler http) : IFireRouter
{
    public bool Matches(FireEnvelop envelop)
    {
        var extensions = envelop.Extensions;
        return extensions.ContainsKey("routing.fabron.io/destination");
    }

    public Task DispatchAsync(FireEnvelop envelop)
    {
        var destination = envelop.Extensions["routing.fabron.io/destination"];
        Guard.IsNotEmpty(destination, nameof(destination));
        return destination.StartsWith("http")
            ? http.SendAsync(new Uri(destination), envelop)
            : ThrowHelper.ThrowArgumentOutOfRangeException<Task>(nameof(destination));
    }
}

