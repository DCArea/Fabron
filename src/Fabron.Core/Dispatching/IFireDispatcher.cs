namespace Fabron.Dispatching;

internal interface IFireDispatcher
{
    Task DispatchAsync(FireEnvelop envelop);
}
