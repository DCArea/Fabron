CREATE TABLE fabron_jobs
(
    key varchar(150) NOT NULL,
    data jsonb NOT NULL,

    PRIMARY KEY(key)
);

CREATE INDEX idx_fabron_jobs_labels ON fabron_jobs USING gin ((data->'Metadata'->'Labels'));

CREATE TABLE fabron_cronjobs
(
    key varchar(150) NOT NULL,
    data jsonb NOT NULL,

    PRIMARY KEY(key)
);

CREATE INDEX idx_fabron_cronjobs_labels ON fabron_cronjobs USING gin ((data->'Metadata'->'Labels'));
