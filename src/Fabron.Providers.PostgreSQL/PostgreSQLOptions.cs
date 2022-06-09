using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fabron.Providers.PostgreSQL
{
    public class PostgreSQLOptions
    {
        public string ConnectionString { get; set; } = default!;
        public string TimedEventTableName { get; set; } = "fabron_timedevents_v1";
        public string CronEventTableName { get; set; } = "fabron_cronevents_v1";
        public string PeriodicEventTableName { get; set; } = "fabron_periodicevents_v1";

        public JsonSerializerOptions JsonSerializerOptions { get; set; } = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            IgnoreReadOnlyProperties = true,
        };
    }
}
