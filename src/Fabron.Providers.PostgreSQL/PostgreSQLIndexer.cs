using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Fabron.Models;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace Fabron.Providers.PostgreSQL
{

    public class PostgreSQLIndexer : IJobIndexer
    {
        private readonly PostgreSQLOptions _options;
        private readonly string _sql_InsertJob;
        private readonly string _sql_DeleteJob;
        private readonly string _sql_InsertCronJob;
        private readonly string _sql_DeleteCronJob;

        public PostgreSQLIndexer(IOptions<PostgreSQLOptions> options)
        {
            _options = options.Value;
            string jobIndexesTableName = options.Value.JobIndexesTableName;
            string cronJobIndexesTableName = options.Value.CronJobIndexesTableName;

            _sql_InsertJob = $@"
INSERT INTO {jobIndexesTableName} (
    key,
    data
)
VALUES (
    @key,
    @data
)
ON CONFLICT (key)
DO
    UPDATE SET data = EXCLUDED.data
";

            _sql_DeleteJob = $@"
DELETE FROM {jobIndexesTableName}
WHERE key = @key
";

            _sql_InsertCronJob = $@"
INSERT INTO {cronJobIndexesTableName} (
    key,
    data
)
VALUES (
    @key,
    @data
)
ON CONFLICT (key)
DO
    UPDATE SET data = EXCLUDED.data
";
            _sql_DeleteCronJob = $@"
DELETE FROM {cronJobIndexesTableName}
WHERE key = @key
";

        }

        public async Task Index(Job job)
        {
            await using var conn = new NpgsqlConnection(_options.ConnectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(_sql_InsertJob, conn);
            cmd.Parameters.AddWithValue("@key", job.Metadata.Key);
            cmd.Parameters.AddWithValue("@data", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(job, _options.JsonSerializerOptions));

            await cmd.ExecuteNonQueryAsync();
        }

        public Task Index(IEnumerable<Job> jobs) => throw new System.NotImplementedException();

        public async Task DeleteJob(string key)
        {
            await using var conn = new NpgsqlConnection(_options.ConnectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(_sql_DeleteJob, conn);
            cmd.Parameters.AddWithValue("@key", key);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task Index(CronJob job)
        {
            await using var conn = new NpgsqlConnection(_options.ConnectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(_sql_InsertCronJob, conn);
            cmd.Parameters.AddWithValue("@key", job.Metadata.Key);
            cmd.Parameters.AddWithValue("@data", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(job, _options.JsonSerializerOptions));

            await cmd.ExecuteNonQueryAsync();
        }

        public Task Index(IEnumerable<CronJob> job) => throw new System.NotImplementedException();

        public async Task DeleteCronJob(string key)
        {
            await using var conn = new NpgsqlConnection(_options.ConnectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(_sql_DeleteCronJob, conn);
            cmd.Parameters.AddWithValue("@key", NpgsqlDbType.Jsonb, key);

            await cmd.ExecuteNonQueryAsync();
        }
    }

}
