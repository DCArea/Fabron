using Fabron.CloudEvents;
using Fabron.Models;

namespace Fabron;

public interface IFabronClient
{
    Task ScheduleTimedEvent<T>(
        string key,
        DateTimeOffset schedule,
        CloudEventTemplate<T> template,
        Dictionary<string, string>? labels = null,
        Dictionary<string, string>? annotations = null);

    Task<TimedEvent<TData>?> GetTimedEvent<TData>(string key);

    Task<TickerStatus> GetTimedEventTickerStatus(string key);

    Task CancelTimedEvent(string key);

    Task ScheduleCronEvent<T>(
        string key,
        string schedule,
        CloudEventTemplate<T> template,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? expirationTime = null,
        bool suspend = false,
        Dictionary<string, string>? labels = null,
        Dictionary<string, string>? annotations = null);

    Task<CronEvent<TData>?> GetCronEvent<TData>(string key);

    Task<TickerStatus> GetCronEventTickerStatus(string key);

    Task CancelCronEvent(string key);

    Task SchedulePeriodicEvent<T>(
        string key,
        CloudEventTemplate<T> template,
        TimeSpan period,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? expirationTime = null,
        bool suspend = false,
        Dictionary<string, string>? labels = null,
        Dictionary<string, string>? annotations = null);

    Task<PeriodicEvent<TData>?> GetPeriodicEvent<TData>(string key);

    Task<TickerStatus> GetPeriodicEventTickerStatus(string key);

    Task CancelPeriodicEvent(string key);
}
