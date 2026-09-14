USE [FidelitasHub];
GO

IF COL_LENGTH('dbo.Employees', 'EnableIdleMonitoring') IS NULL
BEGIN
    ALTER TABLE dbo.Employees
    ADD EnableIdleMonitoring bit NOT NULL
        CONSTRAINT DF_Employees_EnableIdleMonitoring DEFAULT (0);
END
GO

IF OBJECT_ID('dbo.EmployeeIdleSessions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmployeeIdleSessions
    (
        EmployeeIdleSessionId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmployeeId int NOT NULL,
        AttendanceId int NOT NULL,
        IdleStart datetime2 NOT NULL,
        IdleEnd datetime2 NULL,
        DurationSeconds int NOT NULL CONSTRAINT DF_EmployeeIdleSessions_DurationSeconds DEFAULT (0),
        ComputerName nvarchar(100) NULL,
        WindowsUserName nvarchar(200) NULL,
        CONSTRAINT FK_EmployeeIdleSessions_Employees
            FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(EmployeeId) ON DELETE CASCADE,
        CONSTRAINT FK_EmployeeIdleSessions_Attendances
            FOREIGN KEY (AttendanceId) REFERENCES dbo.Attendances(AttendanceId) ON DELETE CASCADE
    );

    CREATE INDEX IX_EmployeeIdleSessions_AttendanceId_IdleEnd
        ON dbo.EmployeeIdleSessions(AttendanceId, IdleEnd);

    CREATE INDEX IX_EmployeeIdleSessions_EmployeeId
        ON dbo.EmployeeIdleSessions(EmployeeId);
END
GO

PRINT 'Employee idle monitoring database update completed.';
GO
