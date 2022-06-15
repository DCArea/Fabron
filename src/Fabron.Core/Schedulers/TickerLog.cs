using System;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace Fabron.Schedulers;

public static partial class TickerLog
{
    [LoggerMessage(
        EventId = 13000,
        Level = LogLevel.Debug,
        Message = "[{key}]: Reminder received, {tickStatus}")]
    public static partial void ReceivedReminder(ILogger logger, string key, TickStatus tickStatus);


    [LoggerMessage(
        EventId = 13100,
        Level = LogLevel.Information,
        Message = "[{key}]: Ticker registered with due time: {dueTime}")]
    public static partial void TickerRegistered(ILogger logger, string key, TimeSpan dueTime);

    [LoggerMessage(
        EventId = 13150,
        Level = LogLevel.Information,
        Message = "[{key}]: Ticking at {now:o}, expected: {expected:o}")]
    public static partial void Ticking(ILogger logger, string key, DateTimeOffset now, DateTimeOffset expected);

    [LoggerMessage(
        EventId = 13200,
        Level = LogLevel.Warning,
        Message = "[{key}]: Unexpected tick at {tickTime:o}, reason: {reason}")]
    public static partial void UnexpectedTick(ILogger logger, string key, DateTimeOffset tickTime, string reason);

    [LoggerMessage(
        EventId = 13250,
        Level = LogLevel.Debug,
        Message = "[{key}]: Dispatching at {now:o}, expeced: {expected:o}")]
    public static partial void Dispatching(ILogger logger, string key, DateTimeOffset now, DateTimeOffset expected);

    [LoggerMessage(
        EventId = 13300,
        Level = LogLevel.Warning,
        Message = "[{key}]: tick missed at {tickTime:o}")]
    public static partial void TickMissed(ILogger logger, string key, DateTimeOffset tickTime);

    [LoggerMessage(
        EventId = 13400,
        Level = LogLevel.Error,
        Message = "[{key}]: error on ticking")]
    public static partial void ErrorOnTicking(ILogger logger, string key, Exception exception);

    [LoggerMessage(
        EventId = 13900,
        Level = LogLevel.Information,
        Message = "[{key}]: Ticker disposed")]
    public static partial void TickerDisposed(ILogger logger, string key);

    [LoggerMessage(
        EventId = 13910,
        Level = LogLevel.Warning,
        Message = "[{key}]: Unregister reminder failed, retry")]
    public static partial void RetryUnregisterReminder(ILogger logger, string key);
}
