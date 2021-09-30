using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using Cassandra.Mapping;
using Fabron.Events;
using Fabron.Stores;
using Microsoft.Extensions.Options;

namespace Fabron.Providers.Cassandra
{
    public class CassandraEventStore : IEventStore
    {
        private readonly ISession _session;

        public CassandraEventStore(ISession session, string eventLogTableName, string consumerOffsetTableName)
        {
            _session = session;

            PreparedCommitEventLog = PrepareCommitEventLog(session, eventLogTableName);
            PreparedGetEventLogs = PrepareGetEventLogs(session, eventLogTableName);
            PreparedClearEventLogs = PrepareClearEventLogs(session, eventLogTableName);

            PreparedGetConsumerOffset = PrepareGetConsumerOffset(session, consumerOffsetTableName);
            PreparedSaveConsumerOffset = PrepareSaveConsumerOffset(session, consumerOffsetTableName);
            PreparedClearConsumerOffset = PrepareClearConsumerOffset(session, consumerOffsetTableName);
        }

        public PreparedStatement PreparedCommitEventLog { get; }
        public static PreparedStatement PrepareCommitEventLog(ISession session, string tableName)
        {
            return session.Prepare(@$"
INSERT INTO {tableName} (
    entity_key,
    version,
    type,
    timestamp,
    data
)
VALUES (
    :entity_key,
    :version,
    :type,
    :timestamp,
    :data
);
            ");
        }
        public async Task CommitEventLog(EventLog eventLog)
        {
            var statement = PreparedCommitEventLog.Bind(new
            {
                entity_key = eventLog.EntityKey,
                version = eventLog.Version,
                type = eventLog.Type,
                timestamp = eventLog.Timestamp,
                data = eventLog.Data
            });
            await _session.ExecuteAsync(statement);
        }

        public PreparedStatement PreparedGetEventLogs { get; }
        public static PreparedStatement PrepareGetEventLogs(ISession session, string tableName)
        {
            return session.Prepare(@$"
SELECT * FROM {tableName}
WHERE entity_key = :entity_key
    AND version >= :min_version
ORDER BY version ASC;
            ");
        }
        public async Task<List<EventLog>> GetEventLogs(string entityKey, long minVersion)
        {
            var statement = PreparedGetEventLogs.Bind(new
            {
                entity_key = entityKey,
                min_version = minVersion
            });
            var mapper = new Mapper(_session);

            var result = await _session.ExecuteAsync(statement);
            var logs = result.GetRows().Select(row => new EventLog(
                (string)row["entity_key"],
                (long)row["version"],
                ((DateTimeOffset)row["timestamp"]).UtcDateTime,
                (string)row["type"],
                (string)row["data"]
            )).ToList();
            return logs;
        }

        public PreparedStatement PreparedClearEventLogs { get; }
        public static PreparedStatement PrepareClearEventLogs(ISession session, string tableName)
        {
            return session.Prepare(@$"
DELETE FROM {tableName}
WHERE
    entity_key = :entity_key
");
        }
        public async Task ClearEventLogs(string entityKey, long maxVersion)
        {
            var statement = PreparedClearEventLogs.Bind(new
            {
                entity_key = entityKey
            });
            await _session.ExecuteAsync(statement);
        }

        public PreparedStatement PreparedGetConsumerOffset { get; }
        public static PreparedStatement PrepareGetConsumerOffset(ISession session, string tableName)
        {
            return session.Prepare(@$"
SELECT * FROM {tableName}
WHERE
    entity_key = :entity_key
");
        }
        public async Task<long> GetConsumerOffset(string entityKey)
        {
            var statement = PreparedGetConsumerOffset.Bind(new
            {
                entity_key = entityKey
            });
            var result = await _session.ExecuteAsync(statement);
            var record = result.GetRows().FirstOrDefault();
            return record is null ? -1L : (long)record["offset"];
        }

        public PreparedStatement PreparedSaveConsumerOffset { get; }
        public static PreparedStatement PrepareSaveConsumerOffset(ISession session, string tableName)
        {
            return session.Prepare(@$"
UPDATE {tableName}
SET
    offset = :offset
WHERE
    entity_key = :entity_key;
");
        }
        public async Task SaveConsumerOffset(string entityKey, long consumerOffset)
        {
            var statement = PreparedSaveConsumerOffset.Bind(new
            {
                entity_key = entityKey
            });
            await _session.ExecuteAsync(statement);
        }

        public PreparedStatement PreparedClearConsumerOffset { get; }
        public static PreparedStatement PrepareClearConsumerOffset(ISession session, string tableName)
        {
            return session.Prepare(@$"
DELETE FROM {tableName}
WHERE
    entity_key = :entity_key
");
        }
        public async Task ClearConsumerOffset(string entityKey)
        {
            var statement = PreparedClearConsumerOffset.Bind(new
            {
                entity_key = entityKey
            });
            await _session.ExecuteAsync(statement);
        }
    }

    public class CassandraJobEventStore : CassandraEventStore, IJobEventStore
    {
        public CassandraJobEventStore(ISession session, IOptions<CassandraOptions> options)
            : base(session, options.Value.JobEventLogsTableName, options.Value.JobEventConsumersTableName)
        {
        }
    }

    public class CassandraCronJobEventStore : CassandraEventStore, ICronJobEventStore
    {
        public CassandraCronJobEventStore(ISession session, IOptions<CassandraOptions> options)
            : base(session, options.Value.CronJobEventLogsTableName, options.Value.CronJobEventConsumersTableName)
        {
        }
    }
}
