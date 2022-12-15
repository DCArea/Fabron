using Fabron.CloudEvents;
using Fabron.Models;
using Microsoft.Toolkit.Diagnostics;

namespace FabronService.EventRouters;

public class AnnotationBasedEventRouter : IEventRouter
{
    private readonly IHttpDestinationHandler _http;

    public AnnotationBasedEventRouter(IHttpDestinationHandler http) => _http = http;

    public bool Matches(ScheduleMetadata metadata, CloudEventEnvelop envelop)
    {
        var annotations = metadata.Annotations;
        return annotations is not null && annotations.ContainsKey("routing.fabron.io/destination");
    }

    public Task DispatchAsync(ScheduleMetadata metadata, CloudEventEnvelop envelop)
    {
        var destination = metadata.Annotations?["routing.fabron.io/destination"];
        Guard.IsNotNull(destination, nameof(destination));
        return destination.StartsWith("http")
            ? _http.SendAsync(new Uri(destination), envelop)
            : ThrowHelper.ThrowArgumentOutOfRangeException<Task>(nameof(destination));
    }
}

