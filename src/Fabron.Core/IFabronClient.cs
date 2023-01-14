using Fabron.Models;

namespace Fabron;

public interface IFabronClient
{
    Task ScheduleTimedEvent(
        string key,
        string? data,
        DateTimeOffset schedule,
        Dictionary<string, string>? extensions = null);

    Task<TimedEvent?> GetTimedEvent(string key);

    Task<TickerStatus> GetTimedEventTickerStatus(string key);

    Task CancelTimedEvent(string key);

    Task ScheduleCronEvent(
        string key,
        string? data,
        string schedule,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? expirationTime = null,
        bool suspend = false,
        Dictionary<string, string>? extensions = null);

    Task<CronEvent?> GetCronEvent(string key);

    Task<TickerStatus> GetCronEventTickerStatus(string key);

    Task CancelCronEvent(string key);

    Task SchedulePeriodicEvent(
        string key,
        string? data,
        TimeSpan period,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? expirationTime = null,
        bool suspend = false,
        Dictionary<string, string>? extensions = null);

    Task<PeriodicEvent?> GetPeriodicEvent(string key);

    Task<TickerStatus> GetPeriodicEventTickerStatus(string key);

    Task CancelPeriodicEvent(string key);
}
