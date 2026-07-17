-- Optional manual script (also auto-created by WebhookService.EnsureTables on first use).
-- Run against the PMS database if you prefer creating tables ahead of time.

IF OBJECT_ID(N'dbo.IntegrationWebhookSubscription', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.IntegrationWebhookSubscription
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        LocationId INT NOT NULL,
        Url NVARCHAR(1000) NOT NULL,
        Secret NVARCHAR(200) NULL,
        Events NVARCHAR(1000) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_IntegrationWebhookSubscription_IsActive DEFAULT(1),
        CreatedDate DATETIME NOT NULL,
        CreatedBy NVARCHAR(100) NULL
    );
END
GO

IF OBJECT_ID(N'dbo.IntegrationWebhookDispatchLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.IntegrationWebhookDispatchLog
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SubscriptionId INT NOT NULL,
        EventType NVARCHAR(100) NOT NULL,
        Payload NVARCHAR(MAX) NULL,
        ResponseCode INT NULL,
        Success BIT NOT NULL,
        AttemptedAt DATETIME NOT NULL,
        ErrorMessage NVARCHAR(MAX) NULL
    );
END
GO

-- Sample Crito client credentials for POST /auth/token
-- UPDATE Client_ID / Client_Secret before using in production.
IF NOT EXISTS (SELECT 1 FROM ClientIntegration WHERE Client_Name = 'Crito')
BEGIN
    INSERT INTO ClientIntegration (Client_Name, Client_ID, Client_Secret)
    VALUES ('Crito', 'crito', 'ChangeThisSecret_Crito_2026');
END
GO
