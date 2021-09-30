CREATE TABLE queries
(
	key ascii,
	text ascii,
	consistency_level ascii,

	PRIMARY KEY(key)
) WITH compression = {
	'class' : 'LZ4Compressor',
	'enabled' : true
};

CREATE TABLE membership
(
	// Will always be zero, used to send all rows to the same partition for the static version to work
	partition_key int,
	version int static,
	address ascii,
	port int,
	generation int,
	silo_name text,
	host_name text,
	status int,
	proxy_port int,
	suspect_times ascii,
	start_time timestamp,
	i_am_alive_time timestamp,

	PRIMARY KEY(partition_key, address, port, generation)
) WITH compression = {
	'class' : 'LZ4Compressor',
	'enabled' : true
};
CREATE INDEX ix_membership_status ON membership(status);

CREATE TABLE reminder
(
	// We will use a custom partitioning scheme for this table since range queries are needed
	partition tinyint,
	grain_hash int,
	grain_id blob,
	reminder_name text,
	start_time timestamp,
	period int,
	// The etag does not in any way need to be a counter. We use a uuid generated on the client side instead.
	etag uuid,

	PRIMARY KEY(partition, grain_hash, grain_id, reminder_name)
) WITH compression = {
	'class' : 'LZ4Compressor',
	'enabled' : true
};

CREATE TABLE storage
(
    grain_type text,
    grain_id blob,
    serializer_code tinyint,
    data blob,
    etag uuid,
    PRIMARY KEY((grain_type, grain_id))
) WITH compression = {
	'class' : 'LZ4Compressor',
	'enabled' : true
};

insert into queries(key, consistency_level, text) values
(
	'UpdateIAmAliveTimeKey','quorum','
	UPDATE membership
	SET
		i_am_alive_time = :i_am_alive_time
	WHERE
		partition_key = 0
		AND address = :address
		AND port = :port
		AND generation = :generation;
');

insert into queries(key, consistency_level, text) values
(
	'InsertMembershipVersionKey','all','
	INSERT INTO membership(
		partition_key,
		version
	)
	VALUES (
		0,
		0
	)
	IF NOT EXISTS;
');

insert into queries(key, consistency_level, text) values
(
	'InsertMembershipKey','all','
	UPDATE membership
	SET
		version = :new_version,
		status = :status,
		start_time = :start_time,
		silo_name = :silo_name,
		host_name = :host_name,
		proxy_port = :proxy_port,
		i_am_alive_time = :i_am_alive_time
	WHERE
		partition_key = 0
		AND address = :address
		AND port = :port
		AND generation = :generation
	IF
		version = :expected_version;
');

insert into queries(key, consistency_level, text) values
(
	'UpdateMembershipKey','all','
	UPDATE membership
	SET
		version = :new_version,
		status = :status,
		suspect_times = :suspect_times,
		i_am_alive_time = :i_am_alive_time
	WHERE
		partition_key = 0
		AND address = :address
		AND port = :port
		AND generation = :generation
	IF
		version = :expected_version;
');

insert into queries(key, consistency_level, text) values
(
	'DeleteMembershipEntryKey','one','
	DELETE FROM
		membership
	WHERE
		partition_key = 0
		AND address = :address
		AND port = :port
		AND generation = :generation;
');

insert into queries(key, consistency_level, text) values
(
	'GatewaysQueryKey','one','
	SELECT
		address,
		proxy_port,
		generation
	FROM
		membership
	WHERE
		partition_key = 0
		AND status = :status
		AND proxy_port > 0
	ALLOW FILTERING;
');

insert into queries(key, consistency_level, text) values
(
	'MembershipReadVersionKey','all','
	SELECT
		version
	FROM
		membership
	WHERE
		partition_key = 0;
');

insert into queries(key, consistency_level, text) values
(
	'MembershipReadRowKey','all','
	SELECT
		version,
		silo_name,
		host_name,
		status,
		proxy_port,
		suspect_times,
		start_time,
		i_am_alive_time
	FROM
		membership
	WHERE
		partition_key = 0
		AND address = :address
		AND port = :port
		AND generation = :generation;
');

insert into queries(key, consistency_level, text) values
(
	'MembershipReadAllKey','all','
	SELECT
		version,
		address,
		port,
		generation,
		silo_name,
		host_name,
		status,
		proxy_port,
		suspect_times,
		start_time,
		i_am_alive_time
	FROM
		membership;
');

insert into queries(key, consistency_level, text) values
(
	'DeleteMembershipTableEntriesKey','all','
	DELETE FROM membership WHERE partition_key = 0;
');

insert into queries(key, consistency_level, text) values
(
	'UpsertReminderRowKey','quorum','
	INSERT INTO reminder (
		partition,
		grain_hash,
		grain_id,
		reminder_name,
		start_time,
		period,
		etag
	)
	VALUES (
		:partition,
		:grain_hash,
		:grain_id,
		:reminder_name,
		:start_time,
		:period,
		:etag
	);
');

insert into queries(key, consistency_level, text) values
(
	'ReadReminderRowsKey','quorum','
	SELECT
		reminder_name,
		start_time,
		period,
		etag
	FROM reminder
	WHERE
		partition = :partition
		AND grain_hash = :grain_hash
		AND grain_id = :grain_id;
');

insert into queries(key, consistency_level, text) values
(
	'ReadReminderRowKey','quorum','
	SELECT
		start_time,
		period,
		etag
	FROM reminder
	WHERE
		partition = :partition
		AND grain_hash = :grain_hash
		AND grain_id = :grain_id
		AND reminder_name = :reminder_name;
');

insert into queries(key, consistency_level, text) values
(
	'ReadRemindersInsideRangeKey','quorum','
	SELECT
		grain_id,
		reminder_name,
		start_time,
		period,
		etag
	FROM reminder
	WHERE
		partition IN :partitions
		AND grain_hash > :grain_hash_start
		AND grain_hash <= :grain_hash_end
');

insert into queries(key, consistency_level, text) values
(
	'ReadRemindersOutsideRangeKey1','quorum','
	SELECT
		grain_id,
		reminder_name,
		start_time,
		period,
		etag
	FROM reminder
	WHERE
		partition IN :partitions
		AND grain_hash > :grain_hash_start;
');
insert into queries(key, consistency_level, text) values
(
	'ReadRemindersOutsideRangeKey2','quorum','
	SELECT
		grain_id,
		reminder_name,
		start_time,
		period,
		etag
	FROM reminder
	WHERE
		partition IN :partitions
		AND grain_hash <= :grain_hash_end;
');

insert into queries(key, consistency_level, text) values
(
	'DeleteReminderRowKey','one','
	DELETE FROM reminder
	WHERE
		partition = :partition
		AND grain_hash = :grain_hash
		AND grain_id = :grain_id
		AND reminder_name = :reminder_name
	IF
		etag = :etag;
');

insert into queries(key, consistency_level, text) values
(
	'DeleteReminderRowsKey','one','
	TRUNCATE reminder;
');

insert into queries(key, consistency_level, text) values
(
	'WriteToStorageKey','one','
	UPDATE storage
	SET
		data = :data,
		serializer_code = :serializer_code,
		etag = :etag
	WHERE
		grain_type = :grain_type
		AND grain_id = :grain_id
	IF
		etag = :expected_etag;
');

insert into queries(key, consistency_level, text) values
(
	'ClearStorageKey','one','
	DELETE FROM storage
	WHERE
		grain_type = :grain_type
		AND grain_id = :grain_id
	IF
		etag = :expected_etag;
');

insert into queries(key, consistency_level, text) values
(
	'ReadFromStorageKey','one','
	SELECT
		data,
		serializer_code,
		etag
	FROM storage
	WHERE
		grain_type = :grain_type
		AND grain_id = :grain_id;
');
