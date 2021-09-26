using Cronos;

namespace Fabron
{
    public class JobOptions
    {
        public bool UseAsynchronousIndexer { get; set; } = true;
    }

    public class CronJobOptions
    {
        public bool UseAsynchronousIndexer { get; set; } = true;
        public CronFormat CronFormat { get; set; } = CronFormat.Standard;
    }

}
