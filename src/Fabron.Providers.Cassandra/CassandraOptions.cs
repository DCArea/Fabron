namespace Fabron.Providers.Cassandra
{
    public class CassandraOptions
    {
        public string ConnectionString { get; set; } = default!;

        public string JobEventLogsTableName { get; set; } = "fabron_job_eventlogs";
        public string JobEventConsumersTableName { get; set; } = "fabron_job_consumers";
        public string CronJobEventLogsTableName { get; set; } = "fabron_cronjob_eventlogs";
        public string CronJobEventConsumersTableName { get; set; } = "fabron_cronjob_consumers";

    }
}
