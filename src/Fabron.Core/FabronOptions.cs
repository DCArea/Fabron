using System;
using Cronos;

namespace Fabron
{
    public class CommonOptions
    {
        public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Utc;
        public bool UseAsynchronousIndexer { get; set; } = true;
    }

    public class JobOptions : CommonOptions
    {
    }

    public class CronJobOptions : CommonOptions
    {
        public CronFormat CronFormat { get; set; } = CronFormat.Standard;
    }

}
