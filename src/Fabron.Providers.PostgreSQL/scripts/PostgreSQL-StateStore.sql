CREATE TABLE fabron_generictimer_v1 (
    key text NOT NULL PRIMARY KEY,
    data jsonb NOT NULL,
    etag text NOT NULL
);

CREATE TABLE fabron_crontimer_v1 (
    key text NOT NULL PRIMARY KEY,
    data jsonb NOT NULL,
    etag text NOT NULL
);

CREATE TABLE fabron_periodictimer_v1 (
    key text NOT NULL PRIMARY KEY,
    data jsonb NOT NULL,
    etag text NOT NULL
);
