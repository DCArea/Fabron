using System;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace Fabron.Schedulers;

public static partial class TickerLog
{
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "[{key}]: Reminder received, {tickStatus}")]
    public static partial void ReceivedReminder(ILogger logger, string key, TickStatus tickStatus);


    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "[{key}]: Ticker registered with due time: {dueTime}")]
    public static partial void TickerRegistered(ILogger logger, string key, TimeSpan dueTime);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "[{key}]: Ticking at {now:o}, expected: {expected:o}")]
    public static partial void Ticking(ILogger logger, string key, DateTimeOffset now, DateTimeOffset expected);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "[{key}]: Unexpected tick at {tickTime:o}, reason: {reason}")]
    public static partial void UnexpectedTick(ILogger logger, string key, DateTimeOffset tickTime, string reason);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "[{key}]: Dispatching at {now:o}, expeced: {expected:o}")]
    public static partial void Dispatching(ILogger logger, string key, DateTimeOffset now, DateTimeOffset expected);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "[{key}]: tick missed at {tickTime:o}")]
    public static partial void TickMissed(ILogger logger, string key, DateTimeOffset tickTime);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "[{key}]: error on ticking")]
    public static partial void ErrorOnTicking(ILogger logger, string key, Exception exception);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "[{key}]: Ticker disposed")]
    public static partial void TickerDisposed(ILogger logger, string key);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "[{key}]: Unregister reminder failed, retry")]
    public static partial void RetryUnregisterReminder(ILogger logger, string key);
}
