CREATE TABLE fabron_jobs_v1 (
    key text NOT NULL PRIMARY KEY,
    data jsonb NOT NULL,
    etag text NOT NULL
);

-- CREATE INDEX idx_fabron_jobs_labels ON fabron_jobs USING gin ((data -> 'Metadata' -> 'Labels'));

CREATE TABLE fabron_cronjobs_v1 (
    key text NOT NULL PRIMARY KEY,
    data jsonb NOT NULL,
    etag text NOT NULL
);

-- CREATE INDEX idx_fabron_cronjobs_labels ON fabron_cronjobs USING gin ((data -> 'Metadata' -> 'Labels'));
