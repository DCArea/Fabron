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

    Task StartGenericTimer(string key);
    Task StopGenericTimer(string key);
    Task DeleteGenericTimer(string key);

    Task ScheduleCronTimer(
        string key,
        string? data,
        string schedule,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? expirationTime = null,
        Dictionary<string, string>? extensions = null);

    Task<CronTimer?> GetCronTimer(string key);

    Task<TickerStatus> GetCronTimerTickerStatus(string key);

    Task StartCronTimer(string key);
    Task StopCronTimer(string key);
    Task DeleteCronTimer(string key);

    Task SchedulePeriodicTimer(
        string key,
        string? data,
        TimeSpan period,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? expirationTime = null,
        Dictionary<string, string>? extensions = null);

    Task<Models.PeriodicTimer?> GetPeriodicTimer(string key);

    Task<TickerStatus> GetPeriodicTimerTickerStatus(string key);

    Task StartPeriodicTimer(string key);
    Task StopPeriodicTimer(string key);
    Task DeletePeriodicTimer(string key);
}
