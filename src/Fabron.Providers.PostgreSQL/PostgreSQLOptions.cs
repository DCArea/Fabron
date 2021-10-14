using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fabron.Providers.PostgreSQL
{
    public class PostgreSQLOptions
    {
        public string ConnectionString { get; set; } = default!;

        public string JobEventLogsTableName { get; set; } = "fabron_job_eventlogs";

        public string CronJobEventLogsTableName { get; set; } = "fabron_cronjob_eventlogs";

        public string JobConsumersTableName { get; set; } = "fabron_job_consumers";

        public string CronJobConsumersTableName { get; set; } = "fabron_cronjob_consumers";

        public string JobIndexesTableName { get; set; } = "fabron_jobs";
        public string CronJobIndexesTableName { get; set; } = "fabron_cronjobs";

        public JsonSerializerOptions JsonSerializerOptions { get; set; } = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            IgnoreReadOnlyProperties = true,
        };

    }

}
