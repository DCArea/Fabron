using Fabron.Models;

namespace Fabron;

public interface IFabronClient
{
    Task ScheduleTimedEvent<TData>(
        string key,
        DateTimeOffset schedule,
        TData data,
        Dictionary<string, string>? extensions = null);

    Task<TimedEvent<TData>?> GetTimedEvent<TData>(string key);

    Task<TickerStatus> GetTimedEventTickerStatus(string key);

    Task CancelTimedEvent(string key);

    Task ScheduleCronEvent<TData>(
        string key,
        string schedule,
        TData data,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? expirationTime = null,
        bool suspend = false,
        Dictionary<string, string>? extensions = null);

    Task<CronEvent<TData>?> GetCronEvent<TData>(string key);

    Task<TickerStatus> GetCronEventTickerStatus(string key);

    Task CancelCronEvent(string key);

    Task SchedulePeriodicEvent<TData>(
        string key,
        TData data,
        TimeSpan period,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? expirationTime = null,
        bool suspend = false,
        Dictionary<string, string>? extensions = null);

    Task<PeriodicEvent<TData>?> GetPeriodicEvent<TData>(string key);

    Task<TickerStatus> GetPeriodicEventTickerStatus(string key);

    Task CancelPeriodicEvent(string key);
}
