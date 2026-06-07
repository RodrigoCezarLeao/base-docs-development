CREATE TABLE IF NOT EXISTS projects (
    id          SERIAL PRIMARY KEY,
    user_id     INT          NOT NULL REFERENCES users(id),
    name        VARCHAR(200) NOT NULL,
    description TEXT,
    is_active   BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS ix_projects_user_id   ON projects (user_id);
CREATE INDEX IF NOT EXISTS ix_projects_is_active ON projects (is_active);
