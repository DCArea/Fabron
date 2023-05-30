# Fabron

This is a distributed timer built on top of Project Orleans.

### How to use it

There are 3 types of timers:
* GenericTimer: fire once at the certain time.
* PeriodicTimer: fire repeatly with a time span. 
* CronTimer: fire by following a cron schedule


#### Define a fire router

```csharp
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
```

#### Configure the server

```csharp

var server = builder.Host.UseFabronServer()
    .AddFireRouter<DefaultFireRouter>();
var client = builder.Host.UseFabronClient(cohosted: true);

server
    .ConfigureOrleans((ctx, siloBuilder) =>
    {
        // configure internal orleans server
    })
```

#### Schedule a timer

```csharp
await client.ScheduleCronTimer(
    "A_Timer_Key",
    new { SomeData = "To be delivered at firing" },
    "0 0 7 * * *", // 7AM every day
    extensions: new(){ { "callback_url": "http://call_this_url_at_firing" } });
```

### Design Details
TBD
