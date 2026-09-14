# FidelitasHub Employee Idle Monitoring

## What this update does

- Adds **Enable Idle Activity Monitoring** to Employee Master.
- Keeps monitoring disabled by default for all existing employees.
- Records only meaningful transitions: **Idle** after 2 minutes without keyboard/mouse input and **Active** when input resumes.
- Ignores idle time while the employee is on a punched Break.
- Closes an open idle session when the employee punches out.
- Adds **Idle** live count, **Current Activity**, and **Total Idle** to Today's Attendance.
- Includes a Windows per-user idle monitor project for shared day/night PCs.

## Database

Run `Database/20260908_AddEmployeeIdleMonitoring.sql` on the production FidelitasHub database, or apply the EF migration:

`20260908090000_AddEmployeeIdleMonitoring.cs`

## Windows monitor

The monitor is intentionally per Windows user. The same physical PC can therefore be used by different day/night shift employees without mixing activity. Each Windows profile has its own monitor settings.

1. Run `IdleMonitor/BUILD_PUBLISH_MONITOR.ps1` on the development PC.
2. Copy the resulting `IdleMonitor/publish` folder to the employee PC together with `Install-IdleMonitor.ps1`.
3. Run `Install-IdleMonitor.ps1` while logged in as the employee.
4. Enter that employee's FidelitasHub Employee Code when prompted.
5. The script creates a Startup-folder shortcut for that Windows user.

The monitor is installed separately inside each Windows user profile. Employees should log off at shift change rather than using Fast User Switching, so a disconnected previous Windows session does not continue running its monitor.

The monitor does not record keystrokes, mouse content, screenshots, or application activity. It only reads Windows' last-input timestamp.

## Server API

`POST /api/idle-monitor/status`

The monitor sends only `Idle` and `Active` transitions. The server uses server-side IST time for all idle session timestamps.
