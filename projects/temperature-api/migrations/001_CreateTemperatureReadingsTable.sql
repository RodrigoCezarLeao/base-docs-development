CREATE TABLE TemperatureReadings (
    Id           INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Location     NVARCHAR(100)     NOT NULL,
    ValueCelsius DECIMAL(5,2)      NOT NULL,
    RecordedAt   DATETIME2         NOT NULL,
    IsActive     BIT               NOT NULL DEFAULT 1,
    CreatedAt    DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt    DATETIME2         NULL
);

CREATE INDEX IX_TemperatureReadings_Location  ON TemperatureReadings (Location);
CREATE INDEX IX_TemperatureReadings_IsActive  ON TemperatureReadings (IsActive);
CREATE INDEX IX_TemperatureReadings_CreatedAt ON TemperatureReadings (CreatedAt DESC);
