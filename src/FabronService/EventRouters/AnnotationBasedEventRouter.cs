using CommunityToolkit.Diagnostics;
using Fabron.Events;

namespace FabronService.EventRouters;

public class AnnotationBasedEventRouter : IEventRouter
{
    private readonly IHttpDestinationHandler _http;

    public AnnotationBasedEventRouter(IHttpDestinationHandler http) => _http = http;

    public bool Matches(FabronEventEnvelop envelop)
    {
        var extensions = envelop.Extensions;
        return extensions.ContainsKey("routing.fabron.io/destination");
    }

    public Task DispatchAsync(FabronEventEnvelop envelop)
    {
        var destination = envelop.Extensions["routing.fabron.io/destination"];
        Guard.IsNotEmpty(destination, nameof(destination));
        return destination.StartsWith("http")
            ? _http.SendAsync(new Uri(destination), envelop)
            : ThrowHelper.ThrowArgumentOutOfRangeException<Task>(nameof(destination));
    }
}

