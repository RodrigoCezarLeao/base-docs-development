CREATE TABLE IF NOT EXISTS documents (
    id         SERIAL PRIMARY KEY,
    project_id INT          NOT NULL REFERENCES projects(id),
    title      VARCHAR(200) NOT NULL,
    file_path  VARCHAR(500) NOT NULL,
    content    TEXT         NOT NULL DEFAULT '',
    canvas_x   FLOAT        NOT NULL DEFAULT 0,
    canvas_y   FLOAT        NOT NULL DEFAULT 0,
    is_active  BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS ix_documents_project_id ON documents (project_id);
CREATE INDEX IF NOT EXISTS ix_documents_is_active  ON documents (is_active);
