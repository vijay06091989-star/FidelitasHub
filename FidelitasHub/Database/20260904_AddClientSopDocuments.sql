/*
   Fidelitas Hub - Client SOP Versioning
   Creates version-controlled SOP records linked to Client Master.
*/

IF OBJECT_ID(N'dbo.ClientSopDocuments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClientSopDocuments
    (
        ClientSopDocumentId INT IDENTITY(1,1) NOT NULL,
        ClientId INT NOT NULL,
        Version INT NOT NULL,
        DisplayFileName NVARCHAR(500) NOT NULL,
        StoredFilePath NVARCHAR(1000) NOT NULL,
        ContentType NVARCHAR(100) NULL,
        FileSizeBytes BIGINT NOT NULL,
        UploadedBy NVARCHAR(200) NOT NULL,
        UploadedOn DATETIME2 NOT NULL,
        IsCurrent BIT NOT NULL CONSTRAINT DF_ClientSopDocuments_IsCurrent DEFAULT (1),

        CONSTRAINT PK_ClientSopDocuments
            PRIMARY KEY (ClientSopDocumentId),

        CONSTRAINT FK_ClientSopDocuments_Clients_ClientId
            FOREIGN KEY (ClientId)
            REFERENCES dbo.Clients(ClientId)
            ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX IX_ClientSopDocuments_ClientId_Version
        ON dbo.ClientSopDocuments(ClientId, Version);
END;
GO

/* Mark the EF migration as applied when this SQL script is used manually. */
IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM dbo.__EFMigrationsHistory
       WHERE MigrationId = N'20260904010000_AddClientSopDocuments'
   )
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'20260904010000_AddClientSopDocuments', N'10.0.9');
END;
GO
