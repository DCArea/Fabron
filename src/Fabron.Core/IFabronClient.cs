using Fabron.Models;

namespace Fabron;

public interface IFabronClient
{
    Task ScheduleGenericTimer(
        string key,
        string? data,
        DateTimeOffset schedule,
        Dictionary<string, string>? extensions = null);

    Task<GenericTimer?> GetGenericTimer(string key);

    Task<TickerStatus> GetGenericTimerTickerStatus(string key);

    Task CancelGenericTimer(string key);

    Task ScheduleCronTimer(
        string key,
        string? data,
        string schedule,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? expirationTime = null,
        bool suspend = false,
        Dictionary<string, string>? extensions = null);

    Task<CronTimer?> GetCronTimer(string key);

    Task<TickerStatus> GetCronTimerTickerStatus(string key);

    Task CancelCronTimer(string key);

    Task SchedulePeriodicTimer(
        string key,
        string? data,
        TimeSpan period,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? expirationTime = null,
        bool suspend = false,
        Dictionary<string, string>? extensions = null);

    Task<Models.PeriodicTimer?> GetPeriodicTimer(string key);

    Task<TickerStatus> GetPeriodicTimerTickerStatus(string key);

    Task CancelPeriodicTimer(string key);
}
