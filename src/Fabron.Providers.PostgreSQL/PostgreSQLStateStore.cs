using System;
using System.Text.Json;
using System.Threading.Tasks;
using Fabron.Providers.PostgreSQL.Exceptions;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace Fabron.Store;

internal class PostgreSQLStateStore
{
    private readonly ILogger _logger;
    private readonly string _connectionString;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly string _sql_upsert;
    private readonly string _sql_update;
    private readonly string _sql_select;
    private readonly string _sql_delete;
    private readonly string _sql_delete_with_etag;

    public PostgreSQLStateStore(ILogger logger, string connectionString, string tableName, JsonSerializerOptions jsonSerializerOptions)
    {
        _logger = logger;
        _connectionString = connectionString;
        _jsonSerializerOptions = jsonSerializerOptions;
        _sql_upsert = $@"
INSERT INTO {tableName} (key, data, etag) VALUES (@key, @data, @etag)
ON CONFLICT (key) DO UPDATE SET data = @data, etag = @etag;";
        _sql_update = $@"
UPDATE {tableName} SET data = @data, etag = @etag
WHERE key = @key AND etag = @expected_etag;";
        _sql_select = $"SELECT data, etag FROM {tableName} WHERE key = @key";
        _sql_delete = $"DELETE FROM {tableName} WHERE key = @key";
        _sql_delete_with_etag = $"DELETE FROM {tableName} WHERE key = @key and etag = @expected_etag";

        Log.StateStoreInitialized(_logger, tableName);
    }

    internal async Task<string> SetStateAsync<TState>(string key, TState data, string? expectedETag)
    {
        Log.SavingState(_logger, key);

        string value = JsonSerializer.Serialize(data, _jsonSerializerOptions);
        string newETag = Guid.NewGuid().ToString();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        NpgsqlCommand cmd;
        if (expectedETag is null)
        {
            cmd = new NpgsqlCommand(_sql_upsert, conn);
            cmd.Parameters.AddWithValue("@key", key);
            cmd.Parameters.AddWithValue("@data", NpgsqlDbType.Jsonb, value);
            cmd.Parameters.AddWithValue("@etag", newETag);
        }
        else
        {
            cmd = new NpgsqlCommand(_sql_update, conn);
            cmd.Parameters.AddWithValue("@key", key);
            cmd.Parameters.AddWithValue("@data", NpgsqlDbType.Jsonb, value);
            cmd.Parameters.AddWithValue("@etag", newETag);
            cmd.Parameters.AddWithValue("@expected_etag", expectedETag);
        }

        int rows = await cmd.ExecuteNonQueryAsync();
        if (rows != 1)
        {
            ThrowHelper.NoItemWasUpdated();
            return null;
        }
        return newETag;
    }

    internal async Task<StateEntry<TState>?> GetStateAsync<TState>(string key)
    {
        Log.GettingState(_logger, key);

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(_sql_select, conn);
        cmd.Parameters.AddWithValue("@key", key);
        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var data = JsonSerializer.Deserialize<TState>(reader.GetString(0), _jsonSerializerOptions)!;
            string etag = reader.GetString(1);
            return new(data, etag);
        }
        return null;
    }

    internal async Task RemoveStateAsync(string key, string? expectedETag)
    {
        Log.DeletingState(_logger, key);

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        NpgsqlCommand cmd;
        if (expectedETag is null)
        {
            cmd = new NpgsqlCommand(_sql_delete, conn);
            cmd.Parameters.AddWithValue("@key", key);
        }
        else
        {
            cmd = new NpgsqlCommand(_sql_delete_with_etag, conn);
            cmd.Parameters.AddWithValue("@key", key);
            cmd.Parameters.AddWithValue("@expected_etag", expectedETag);
        }

        int rows = await cmd.ExecuteNonQueryAsync();
        if (rows != 1 && expectedETag is not null)
        {
            ThrowHelper.NoItemWasUpdated();
            return;
        }
    }
}

public static partial class Log
{
    [LoggerMessage(
        EventId = 10000,
        Level = LogLevel.Information,
        Message = "Initialized PostgreSQL state store ({tableName})")]
    public static partial void StateStoreInitialized(ILogger logger, string tableName);

    [LoggerMessage(
        EventId = 10100,
        Level = LogLevel.Debug,
        Message = "Saving state({key}) in PostgreSQL")]
    public static partial void SavingState(ILogger logger, string key);

    [LoggerMessage(
        EventId = 10200,
        Level = LogLevel.Debug,
        Message = "Getting state({key}) from PostgreSQL")]
    public static partial void GettingState(ILogger logger, string key);

    [LoggerMessage(
        EventId = 10300,
        Level = LogLevel.Debug,
        Message = "Deleting state({key}) from PostgreSQL")]
    public static partial void DeletingState(ILogger logger, string key);
}

