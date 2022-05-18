using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Fabron.Contracts;
using Fabron.Mando;
using Fabron.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace Fabron.Providers.PostgreSQL;

public class PostgreSQLQuerier : IFabronQuerier
{
    private readonly string _connectionString;
    private readonly PostgreSQLOptions _options;

    public PostgreSQLQuerier(IOptions<PostgreSQLOptions> options, ILogger<PostgreSQLQuerier> logger)
    {
        _connectionString = options.Value.ConnectionString;
        _options = options.Value;
    }

    public async Task<List<Job<TCommand, TResult>>> FindJobByLabelsAsync<TCommand, TResult>(
        string @namespace,
        Dictionary<string, string> labels,
        int skip = 0,
        int take = 20) where TCommand : ICommand<TResult>
    {
        var filter = new
        {
            Metadata = new
            {
                Namespace = @namespace,
                Labels = labels
            }
        };

        var data = await BasicQueryAsync(
            _options.JobTableName,
            filter,
            skip,
            take
        );

        var results = data.Select(x => JsonSerializer.Deserialize<Job>(x, _options.JsonSerializerOptions))
            .Where(x => x != null)
            .Select(x => x!.Map<TCommand, TResult>())
            .ToList();
        return results;
    }

    public async Task<List<CronJob<TCommand>>> FindCronJobByLabelsAsync<TCommand>(
        string @namespace,
        Dictionary<string, string> labels,
        int skip = 0,
        int take = 20) where TCommand : ICommand
    {
        var filter = new
        {
            Metadata = new
            {
                Namespace = @namespace,
                Labels = labels
            }
        };

        var data = await BasicQueryAsync(
            _options.JobTableName,
            filter,
            skip,
            take
        );

        var results = data.Select(x => JsonSerializer.Deserialize<CronJob>(x, _options.JsonSerializerOptions))
            .Where(x => x != null)
            .Select(x => x!.Map<TCommand>())
            .ToList();
        return results;
    }

    public async Task<List<Job<TCommand, TResult>>> FindJobByOwnerAsync<TCommand, TResult>(
        string @namespace,
        OwnerReference owner,
        int skip = 0,
        int take = 20) where TCommand : ICommand<TResult>
    {
        var data = await BasicQueryAsync(
            _options.JobTableName,
            new
            {
                Metadata = new
                {
                    Namespace = @namespace,
                    Owner = owner
                }
            },
            skip,
            take
        );

        var results = data.Select(x => JsonSerializer.Deserialize<Job>(x, _options.JsonSerializerOptions))
            .Where(x => x != null)
            .Select(x => x!.Map<TCommand, TResult>())
            .ToList();
        return results;
    }

    protected virtual async Task<List<string>> BasicQueryAsync<T>(
        string tableName,
        T? filter,
        int skip,
        int take)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var results = new List<string>(take);

        var cmd = new NpgsqlCommand($@"
SELECT data FROM {tableName}
    WHERE data @> $1
    ORDER BY key DESC
    LIMIT $2
    OFFSET $3
        ", conn)
        {
            Parameters =
            {
                new NpgsqlParameter<string>{
                    NpgsqlDbType=NpgsqlDbType.Jsonb,
                    TypedValue = JsonSerializer.Serialize(filter, _options.JsonSerializerOptions)
                },
                new NpgsqlParameter<int>{ TypedValue = take },
                new NpgsqlParameter<int>{ TypedValue = skip },
            }
        };

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }
        return results;
    }
}
