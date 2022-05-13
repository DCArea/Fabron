using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fabron.Providers.PostgreSQL
{
    public class PostgreSQLOptions
    {
        public string ConnectionString { get; set; } = default!;
        public string JobTableName { get; set; } = "fabron_jobs_v1";
        public string CronJobTableName { get; set; } = "fabron_cronjobs_v1";

        public JsonSerializerOptions JsonSerializerOptions { get; set; } = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            IgnoreReadOnlyProperties = true,
        };

    }

}
