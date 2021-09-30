CREATE TABLE fabron_job_eventlogs
(
    entity_key ascii,
    version bigint,
    type ascii,
    timestamp timestamp,
    data text,

    PRIMARY KEY(entity_key, version)
) WITH compression = {
    'class' : 'LZ4Compressor',
    'enabled' : true
};

CREATE TABLE fabron_cronjob_eventlogs
(
    entity_key ascii,
    version bigint,
    type ascii,
    timestamp timestamp,
    data text,

    PRIMARY KEY(entity_key, version)
) WITH compression = {
    'class' : 'LZ4Compressor',
    'enabled' : true
};

CREATE TABLE fabron_job_consumers
(
    entity_key ascii,
    offset bigint,

    PRIMARY KEY(entity_key)
) WITH compression = {
    'class' : 'LZ4Compressor',
    'enabled' : true
};

CREATE TABLE fabron_cronjob_consumers
(
    entity_key ascii,
    offset bigint,

    PRIMARY KEY(entity_key)
) WITH compression = {
    'class' : 'LZ4Compressor',
    'enabled' : true
};

