namespace Fabron.Dispatching;

/// <summary>
/// Fire router
/// </summary>
public interface IFireRouter
{
    /// <summary>
    /// Determine if this router should be applied.
    /// </summary>
    /// <param name="envelop">Fire envelop</param>
    /// <returns></returns>
    bool Matches(FireEnvelop envelop);

    /// <summary>
    /// Dispatch fire envelop
    /// </summary>
    /// <param name="envelop">Fire envelop</param>
    /// <returns></returns>
    Task DispatchAsync(FireEnvelop envelop);
}
