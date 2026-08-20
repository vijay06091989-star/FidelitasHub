BEGIN TRANSACTION;
CREATE TABLE [SystemSettings] (
    [Id] int NOT NULL IDENTITY,
    [SettingKey] nvarchar(100) NOT NULL,
    [SettingValue] nvarchar(500) NOT NULL,
    [Description] nvarchar(500) NULL,
    CONSTRAINT [PK_SystemSettings] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260727074911_AddSystemSettings', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [EmailTemplates] (
    [Id] int NOT NULL IDENTITY,
    [TemplateCode] nvarchar(50) NOT NULL,
    [TemplateName] nvarchar(100) NOT NULL,
    [Subject] nvarchar(200) NOT NULL,
    [Body] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [ModifiedOn] datetime2 NULL,
    CONSTRAINT [PK_EmailTemplates] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260728051210_AddEmailTemplates', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EmailTemplates]') AND [c].[name] = N'TemplateName');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [EmailTemplates] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [EmailTemplates] ALTER COLUMN [TemplateName] nvarchar(150) NOT NULL;

DECLARE @var1 nvarchar(max);
SELECT @var1 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EmailTemplates]') AND [c].[name] = N'TemplateCode');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [EmailTemplates] DROP CONSTRAINT ' + @var1 + ';');
ALTER TABLE [EmailTemplates] ALTER COLUMN [TemplateCode] nvarchar(100) NOT NULL;

DECLARE @var2 nvarchar(max);
SELECT @var2 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EmailTemplates]') AND [c].[name] = N'Subject');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [EmailTemplates] DROP CONSTRAINT ' + @var2 + ';');
ALTER TABLE [EmailTemplates] ALTER COLUMN [Subject] nvarchar(250) NOT NULL;

ALTER TABLE [EmailTemplates] ADD [Category] nvarchar(100) NOT NULL DEFAULT N'';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260728070816_AddEmailTemplateCategory', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Employees] ADD [PasswordLastChanged] datetime2 NOT NULL DEFAULT (GETDATE());

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260729091149_AddPasswordLastChanged', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Employees] ADD [ReportingManagerId] int NULL;

ALTER TABLE [Employees] ADD [ReportingTeamLeaderId] int NULL;

CREATE INDEX [IX_Employees_ReportingManagerId] ON [Employees] ([ReportingManagerId]);

CREATE INDEX [IX_Employees_ReportingTeamLeaderId] ON [Employees] ([ReportingTeamLeaderId]);

ALTER TABLE [Employees] ADD CONSTRAINT [FK_Employees_Employees_ReportingManagerId] FOREIGN KEY ([ReportingManagerId]) REFERENCES [Employees] ([EmployeeId]) ON DELETE NO ACTION;

ALTER TABLE [Employees] ADD CONSTRAINT [FK_Employees_Employees_ReportingTeamLeaderId] FOREIGN KEY ([ReportingTeamLeaderId]) REFERENCES [Employees] ([EmployeeId]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260817054845_AddReportingStructure', N'10.0.9');

COMMIT;
GO

