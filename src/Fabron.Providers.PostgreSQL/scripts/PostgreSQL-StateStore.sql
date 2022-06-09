CREATE TABLE fabron_timedevents_v1 (
    key text NOT NULL PRIMARY KEY,
    data jsonb NOT NULL,
    etag text NOT NULL
);

-- CREATE INDEX on fabron_timedevents_v1 USING GIN (data jsonb_path_ops);

CREATE TABLE fabron_cronevents_v1 (
    key text NOT NULL PRIMARY KEY,
    data jsonb NOT NULL,
    etag text NOT NULL
);

-- CREATE INDEX on fabron_cronevents_v1 USING GIN (data jsonb_path_ops);

CREATE TABLE fabron_periodicevents_v1 (
    key text NOT NULL PRIMARY KEY,
    data jsonb NOT NULL,
    etag text NOT NULL
);
