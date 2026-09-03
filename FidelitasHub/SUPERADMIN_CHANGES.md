# SuperAdmin Update - 03 Sep 2026

This source package includes the SuperAdmin changes requested for Fidelitas Hub.

## SuperAdmin behavior

- SuperAdmin has the same application access and visibility as Admin.
- SuperAdmin does not require a Shift.
- SuperAdmin does not require a Reporting Manager.
- SuperAdmin does not require a Reporting Team Leader.
- SuperAdmin is excluded from Today's Attendance.
- SuperAdmin is excluded from Attendance Register and attendance exports.
- SuperAdmin is excluded from attendance correction.
- SuperAdmin cannot Punch In, Break, Resume, or Punch Out.
- SuperAdmin is excluded from attendance processing and the Admin Dashboard's Present Today attendance metric.
- Existing Admin/SuperAdmin leave approval access remains enabled.

## Database deployment

Before using SuperAdmin in a database where `Employees.ShiftId` is still NOT NULL, apply one of the following:

### Option A - SQL script
Run:

`Database/20260903_MakeEmployeeShiftOptional.sql`

### Option B - EF Core migration
The package includes migration:

`20260903070000_MakeEmployeeShiftOptional`

Apply the migration using your normal EF Core deployment process.

## Important deployment note

The source project does not automatically run EF migrations at application startup. Publishing the application alone will not change the production database schema. The ShiftId change must be applied to production separately before creating a SuperAdmin account.
