CREATE TABLE IF NOT EXISTS document_connections (
    id                 SERIAL PRIMARY KEY,
    project_id         INT         NOT NULL REFERENCES projects(id),
    source_document_id INT         NOT NULL REFERENCES documents(id),
    target_document_id INT         NOT NULL REFERENCES documents(id),
    label              VARCHAR(200),
    is_active          BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at         TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_document_connections_project_id         ON document_connections (project_id);
CREATE INDEX IF NOT EXISTS ix_document_connections_source_document_id ON document_connections (source_document_id);
CREATE INDEX IF NOT EXISTS ix_document_connections_target_document_id ON document_connections (target_document_id);
CREATE INDEX IF NOT EXISTS ix_document_connections_is_active          ON document_connections (is_active);
