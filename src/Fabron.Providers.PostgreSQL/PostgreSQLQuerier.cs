using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Fabron.Models;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Fabron.Providers.PostgreSQL
{

    public class PostgreSQLQuerier : IJobQuerier
    {
        private readonly string _sql_GetJobByKey;
        private readonly string _sql_GetJobByLabel;
        private readonly string _sql_GetCronJobByKey;
        private readonly string _sql_GetCronJobByLabel;
        private readonly PostgreSQLOptions _options;

        public PostgreSQLQuerier(IOptions<PostgreSQLOptions> options)
        {
            _options = options.Value;
            string jobIndexesTableName = options.Value.JobIndexesTableName;
            string cronJobIndexesTableName = options.Value.CronJobIndexesTableName;

            _sql_GetJobByKey = $@"
SELECT data FROM {jobIndexesTableName}
WHERE key = @key
";
            _sql_GetJobByLabel = $@"
SELECT data FROM {jobIndexesTableName}
WHERE data @> @query;
";

            _sql_GetCronJobByKey = $@"
SELECT data FROM {cronJobIndexesTableName}
WHERE key = @key
";
            _sql_GetCronJobByLabel = $@"
SELECT data FROM {cronJobIndexesTableName}
WHERE data @> @query;
";

        }

        public async Task<Job?> GetJobByKey(string key)
        {
            await using var conn = new NpgsqlConnection(_options.ConnectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(_sql_GetJobByKey, conn);
            cmd.Parameters.AddWithValue("@key", key);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return JsonSerializer.Deserialize<Job>(reader.GetString(0), _options.JsonSerializerOptions);
            }
            return null;
        }

        public async Task<List<Job>> GetJobByLabel(string labelName, string labelValue)
        {
            var result = new List<Job>();

            await using var conn = new NpgsqlConnection(_options.ConnectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(_sql_GetJobByLabel, conn);
            var query = new
            {
                Metadata = new
                {
                    Labels = new Dictionary<string, string> {
                        { labelName, labelValue }
                    }
                }
            };
            cmd.Parameters.AddWithValue("@query", NpgsqlTypes.NpgsqlDbType.Jsonb, JsonSerializer.Serialize(query));

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add(JsonSerializer.Deserialize<Job>(reader.GetString(0), _options.JsonSerializerOptions)!);

            return result;
        }

        public async Task<List<Job>> GetJobByLabels(IEnumerable<(string, string)> labels)
        {
            var result = new List<Job>();

            await using var conn = new NpgsqlConnection(_options.ConnectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(_sql_GetJobByLabel, conn);
            var query = new
            {
                Metadata = new
                {
                    Labels = labels.ToDictionary(x => x.Item1, x => x.Item2)
                }
            };
            cmd.Parameters.AddWithValue("@query", NpgsqlTypes.NpgsqlDbType.Jsonb, JsonSerializer.Serialize(query));

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add(JsonSerializer.Deserialize<Job>(reader.GetString(0), _options.JsonSerializerOptions)!);

            return result;
        }

        public async Task<CronJob?> GetCronJobByKey(string key)
        {
            await using var conn = new NpgsqlConnection(_options.ConnectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(_sql_GetCronJobByKey, conn);
            cmd.Parameters.AddWithValue("@key", key);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return JsonSerializer.Deserialize<CronJob>(reader.GetString(0), _options.JsonSerializerOptions);
            }
            return null;
        }

        public async Task<List<CronJob>> GetCronJobByLabel(string labelName, string labelValue)
        {
            var result = new List<CronJob>();

            await using var conn = new NpgsqlConnection(_options.ConnectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(_sql_GetCronJobByLabel, conn);
            var query = new
            {
                Metadata = new
                {
                    Labels = new Dictionary<string, string> {
                        { labelName, labelValue }
                    }
                }
            };
            cmd.Parameters.AddWithValue("@query", NpgsqlTypes.NpgsqlDbType.Jsonb, JsonSerializer.Serialize(query));

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add(JsonSerializer.Deserialize<CronJob>(reader.GetString(0), _options.JsonSerializerOptions)!);

            return result;
        }

        public async Task<List<CronJob>> GetCronJobByLabels(IEnumerable<(string, string)> labels)
        {
            var result = new List<CronJob>();

            await using var conn = new NpgsqlConnection(_options.ConnectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(_sql_GetCronJobByLabel, conn);
            var query = new
            {
                Metadata = new
                {
                    Labels = labels.ToDictionary(x => x.Item1, x => x.Item2)
                }
            };
            cmd.Parameters.AddWithValue("@query", NpgsqlTypes.NpgsqlDbType.Jsonb, JsonSerializer.Serialize(query));

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add(JsonSerializer.Deserialize<CronJob>(reader.GetString(0), _options.JsonSerializerOptions)!);

            return result;
        }
    }

}
