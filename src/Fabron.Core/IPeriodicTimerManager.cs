namespace Fabron;

/// <summary>
/// Periodic timer manager
/// </summary>
public interface IPeriodicTimerManager : ITimerManager<PeriodicTimer>
{
    /// <summary>
    /// Schedule a periodic timer
    /// </summary>
    /// <param name="key">Timer key</param>
    /// <param name="data">Data to be deliver when the timer fires</param>
    /// <param name="period">The cron expression</param>
    /// <param name="notBefore">The timer will not be fired before this time</param>
    /// <param name="notAfter">The timer will be expired after this time</param>
    /// <param name="extensions">Extension fields</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public Task Schedule(
        string key,
        string? data,
        TimeSpan period,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? notAfter = null,
        Dictionary<string, string>? extensions = null);
}
