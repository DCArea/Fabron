using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fabron.Providers.PostgreSQL
{
    public class PostgreSQLOptions
    {
        public string ConnectionString { get; set; } = default!;
        public string GenericTimerTableName { get; set; } = "fabron_generictimer_v1";
        public string CronTimerTableName { get; set; } = "fabron_crontimer_v1";
        public string PeriodicTimerTableName { get; set; } = "fabron_periodictimer_v1";

        public JsonSerializerOptions JsonSerializerOptions { get; set; } = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            IgnoreReadOnlyProperties = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    }
}
