CREATE TABLE IF NOT EXISTS temperature_readings (
    id            SERIAL PRIMARY KEY,
    location      VARCHAR(100)   NOT NULL,
    value_celsius NUMERIC(5, 2)  NOT NULL,
    recorded_at   TIMESTAMPTZ    NOT NULL,
    is_active     BOOLEAN        NOT NULL DEFAULT TRUE,
    created_at    TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    updated_at    TIMESTAMPTZ    NULL
);

CREATE INDEX IF NOT EXISTS ix_temperature_readings_location
    ON temperature_readings (location);

CREATE INDEX IF NOT EXISTS ix_temperature_readings_is_active
    ON temperature_readings (is_active);

CREATE INDEX IF NOT EXISTS ix_temperature_readings_recorded_at
    ON temperature_readings (recorded_at DESC);
