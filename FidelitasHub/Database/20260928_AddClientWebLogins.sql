/*
    Fidelitas Hub
    Client Web Portal Logins
    28-Sep-2026

    Run once against the Fidelitas Hub production database
    before using the new Client Web Logins feature.

    Passwords and security questions are stored encrypted by the application.
*/

IF OBJECT_ID(N'dbo.ClientWebLogins', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClientWebLogins
    (
        ClientWebLoginId INT IDENTITY(1,1) NOT NULL,
        ClientId INT NOT NULL,
        Website NVARCHAR(250) NOT NULL,
        Url NVARCHAR(1000) NOT NULL,
        Username NVARCHAR(250) NOT NULL,
        EncryptedPassword NVARCHAR(MAX) NOT NULL,
        EncryptedSecurityQuestions NVARCHAR(MAX) NULL,
        CreatedOn DATETIME2 NOT NULL,
        CreatedBy NVARCHAR(200) NULL,
        ModifiedOn DATETIME2 NULL,
        ModifiedBy NVARCHAR(200) NULL,

        CONSTRAINT PK_ClientWebLogins
            PRIMARY KEY (ClientWebLoginId),

        CONSTRAINT FK_ClientWebLogins_Clients_ClientId
            FOREIGN KEY (ClientId)
            REFERENCES dbo.Clients(ClientId)
            ON DELETE CASCADE
    );

    CREATE INDEX IX_ClientWebLogins_ClientId
        ON dbo.ClientWebLogins(ClientId);
END
GO
