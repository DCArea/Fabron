using System.Text.Json;
using System.Text.Json.Serialization;
using Cronos;

namespace Fabron
{
    public class SchedulerOptions
    {
        public JsonSerializerOptions JsonSerializerOptions { get; set; }
            = new(JsonSerializerDefaults.Web)
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };

        public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Utc;
        public TimeSpan TickerInterval { get; set; } = TimeSpan.FromMinutes(2);
    }

    public class FabronClientOptions : SchedulerOptions
    {
    }

    public class SimpleSchedulerOptions : SchedulerOptions
    {
        public SimpleSchedulerOptions() => TickerInterval = TimeSpan.FromMinutes(1);
    }

    public class CronSchedulerOptions : SchedulerOptions
    {
        public CronFormat CronFormat { get; set; } = CronFormat.Standard;
    }

    public class PeriodicSchedulerOptions : SchedulerOptions
    {
    }

}
