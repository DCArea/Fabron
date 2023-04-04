using Microsoft.Extensions.Logging;

namespace Fabron.Store;

public static partial class Log
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Initialized PostgreSQL state store ({tableName})")]
    public static partial void StateStoreInitialized(ILogger logger, string tableName);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Saving state({key}), etag: {etag}")]
    public static partial void SavingState(ILogger logger, string key, string? etag);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Getting state({key})")]
    public static partial void GettingState(ILogger logger, string key);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Deleting state({key}), etag: {etag}")]
    public static partial void DeletingState(ILogger logger, string key, string? etag);
}

