CREATE TABLE fabron_jobs_v1 (
    key text NOT NULL PRIMARY KEY,
    data jsonb NOT NULL,
    etag text NOT NULL
);

CREATE INDEX on fabron_jobs_v1 USING GIN (data jsonb_path_ops);

CREATE TABLE fabron_cronjobs_v1 (
    key text NOT NULL PRIMARY KEY,
    data jsonb NOT NULL,
    etag text NOT NULL
);

CREATE INDEX on fabron_cronjobs_v1 USING GIN (data jsonb_path_ops);
