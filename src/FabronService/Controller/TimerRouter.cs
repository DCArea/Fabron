using CloudEventDotNet;
using Fabron.Dispatching;

namespace FabronService.Controller;

public sealed class TimerRouter : IFireRouter
{
    private readonly ICloudEventPubSub _pubsub;

    public TimerRouter(ICloudEventPubSub pubsub) => _pubsub = pubsub;

    public bool Matches(FireEnvelop envelop)
        => true;

    public Task DispatchAsync(FireEnvelop envelop)
    {
        var id = $"{envelop.Source}/{envelop.Time.ToUnixTimeSeconds()}";
        return _pubsub.PublishAsync(new TimerFired(envelop), id, envelop.Time);
    }
}

[CloudEvent(Type = "timer.fired")]
public record TimerFired(FireEnvelop Envelop);
