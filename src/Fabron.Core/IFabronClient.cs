using Fabron.Models;

namespace Fabron;

public interface IFabronClient
{
    /// <summary>
    /// Schedule a generic timer
    /// </summary>
    /// <param name="key">Timer key</param>
    /// <param name="data">Data to be deliver when the timer fires</param>
    /// <param name="schedule">Time to fire</param>
    /// <param name="extensions">Extension fields</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task ScheduleGenericTimer(
        string key,
        string? data,
        DateTimeOffset schedule,
        Dictionary<string, string>? extensions = null);

    /// <summary>
    /// Get a generic timer
    /// </summary>
    /// <param name="key">Timer key</param>
    /// <returns>The timer, null if not exists</returns>
    Task<GenericTimer?> GetGenericTimer(string key);

    /// <summary>
    /// Start the ticker of a generic timer
    /// </summary>
    /// <param name="key">Timer key</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task StartGenericTimer(string key);

    /// <summary>
    /// Stop the ticker of a generic timer
    /// </summary>
    /// <param name="key">Timer key</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task StopGenericTimer(string key);

    /// <summary>
    /// Delete a generic timer
    /// </summary>
    /// <param name="key">Timer key</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task DeleteGenericTimer(string key);

    /// <summary>
    /// Schedule a cron timer
    /// </summary>
    /// <param name="key">Timer key</param>
    /// <param name="data">Data to be deliver when the timer fires</param>
    /// <param name="schedule">The cron expression</param>
    /// <param name="notBefore">The timer will not be fired before this time</param>
    /// <param name="expirationTime">The timer will be expired after this time</param>
    /// <param name="extensions">Extension fields</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task ScheduleCronTimer(
        string key,
        string? data,
        string schedule,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? expirationTime = null,
        Dictionary<string, string>? extensions = null);

    /// <summary>
    /// Get a cron timer
    /// </summary>
    /// <param name="key">Timer key</param>
    /// <returns>The cron timer</returns>
    Task<CronTimer?> GetCronTimer(string key);

    /// <summary>
    /// Start a cron timer
    /// </summary>
    /// <param name="key">Timer key</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task StartCronTimer(string key);

    /// <summary>
    /// Stop a cron timer
    /// </summary>
    /// <param name="key">Timer key</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task StopCronTimer(string key);

    /// <summary>
    /// Delete a cron timer
    /// </summary>
    /// <param name="key">Timer key</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task DeleteCronTimer(string key);

    /// <summary>
    /// Schedule a periodic timer
    /// </summary>
    /// <param name="key">Timer key</param>
    /// <param name="data">Data to be deliver when the timer fires</param>
    /// <param name="period">The cron expression</param>
    /// <param name="notBefore">The timer will not be fired before this time</param>
    /// <param name="expirationTime">The timer will be expired after this time</param>
    /// <param name="extensions">Extension fields</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task SchedulePeriodicTimer(
        string key,
        string? data,
        TimeSpan period,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? expirationTime = null,
        Dictionary<string, string>? extensions = null);

    /// <summary>
    /// Get a periodic timer
    /// </summary>
    /// <param name="key">Timer key</param>
    /// <returns>The periodic timer</returns>
    Task<Models.PeriodicTimer?> GetPeriodicTimer(string key);

    /// <summary>
    /// Start a periodic timer
    /// </summary>
    /// <param name="key">Timer key</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task StartPeriodicTimer(string key);

    /// <summary>
    /// Stop a periodic timer
    /// </summary>
    /// <param name="key">Timer key</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task StopPeriodicTimer(string key);

    /// <summary>
    /// Delete a periodic timer
    /// </summary>
    /// <param name="key">Timer key</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task DeletePeriodicTimer(string key);
}
