namespace Fabron.Dispatching;

public interface IFireRouter
{
    bool Matches(FireEnvelop envelop);
    Task DispatchAsync(FireEnvelop envelop);
}
