using Fabron.Models;

namespace Fabron;

/// <summary>
/// Generic timer manager
/// </summary>
public interface IGenericTimerManager : ITimerManager<GenericTimer>
{
    /// <summary>
    /// Schedule a generic timer
    /// </summary>
    /// <param name="key">Timer key</param>
    /// <param name="data">Data to be deliver when the timer fires</param>
    /// <param name="schedule">Time to fire</param>
    /// <param name="extensions">Extension fields</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public Task Schedule(
        string key,
        string? data,
        DateTimeOffset schedule,
        Dictionary<string, string>? extensions = null);
}
