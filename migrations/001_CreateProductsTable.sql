CREATE TABLE Products (
    Id          INT IDENTITY(1,1)   NOT NULL PRIMARY KEY,
    Name        NVARCHAR(200)       NOT NULL,
    Description NVARCHAR(1000)      NOT NULL DEFAULT '',
    Price       DECIMAL(18,2)       NOT NULL,
    Stock       INT                 NOT NULL DEFAULT 0,
    IsActive    BIT                 NOT NULL DEFAULT 1,
    CreatedAt   DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt   DATETIME2           NULL
);

CREATE INDEX IX_Products_IsActive   ON Products (IsActive);
CREATE INDEX IX_Products_CreatedAt  ON Products (CreatedAt DESC);
