using CommunityToolkit.Diagnostics;
using Fabron.Dispatching;

namespace FabronService.FireRouters;

public class AnnotationBasedFireRouter : IFireRouter
{
    private readonly IHttpDestinationHandler _http;

    public AnnotationBasedFireRouter(IHttpDestinationHandler http) => _http = http;

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
            ? _http.SendAsync(new Uri(destination), envelop)
            : ThrowHelper.ThrowArgumentOutOfRangeException<Task>(nameof(destination));
    }
}

