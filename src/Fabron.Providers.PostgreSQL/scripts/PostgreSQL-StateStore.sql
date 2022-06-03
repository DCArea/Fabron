CREATE TABLE fabron_timedeventsv1 (
    key text NOT NULL PRIMARY KEY,
    data jsonb NOT NULL,
    etag text NOT NULL
);

-- CREATE INDEX on fabron_timedeventsv1 USING GIN (data jsonb_path_ops);

CREATE TABLE fabron_cronevents_v1 (
    key text NOT NULL PRIMARY KEY,
    data jsonb NOT NULL,
    etag text NOT NULL
);

-- CREATE INDEX on fabron_cronevents_v1 USING GIN (data jsonb_path_ops);
