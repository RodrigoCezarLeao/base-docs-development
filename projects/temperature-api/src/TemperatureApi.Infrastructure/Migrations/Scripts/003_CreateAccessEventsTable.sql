CREATE TABLE IF NOT EXISTS access_events (
    id           BIGSERIAL    PRIMARY KEY,
    user_id      INTEGER,
    ip           VARCHAR(64)  NOT NULL,
    user_agent   TEXT,
    browser      VARCHAR(100),
    os           VARCHAR(100),
    device_type  VARCHAR(100),
    method       VARCHAR(10)  NOT NULL,
    path         VARCHAR(500) NOT NULL,
    status_code  INTEGER      NOT NULL,
    country      VARCHAR(100), -- reserved for future geo-IP enrichment
    city         VARCHAR(100),
    occurred_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_access_events_user_id ON access_events (user_id);
CREATE INDEX IF NOT EXISTS ix_access_events_occurred_at ON access_events (occurred_at DESC);
