CREATE TABLE IF NOT EXISTS consents (
    id             BIGSERIAL   PRIMARY KEY,
    user_id        INTEGER,
    decision       VARCHAR(20) NOT NULL,
    policy_version VARCHAR(20) NOT NULL,
    ip             VARCHAR(64),
    occurred_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_consents_user_id ON consents (user_id);
