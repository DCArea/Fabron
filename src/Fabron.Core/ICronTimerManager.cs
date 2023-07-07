using Fabron.Models;

namespace Fabron;

/// <summary>
/// Cron timer manager
/// </summary>
public interface ICronTimerManager : ITimerManager<CronTimer>
{
    /// <summary>
    /// Schedule a cron timer
    /// </summary>
    /// <param name="key">Timer key</param>
    /// <param name="data">Data to be deliver when the timer fires</param>
    /// <param name="cron">The cron expression</param>
    /// <param name="notBefore">The timer will not be fired before this time</param>
    /// <param name="notAfter">The timer will be expired after this time</param>
    /// <param name="extensions">Extension fields</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public Task Schedule(
        string key,
        string? data,
        string cron,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? notAfter = null,
        Dictionary<string, string>? extensions = null);
}
