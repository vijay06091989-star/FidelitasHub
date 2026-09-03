/*
   Fidelitas Hub - SuperAdmin support
   Makes ShiftId optional so SuperAdmin accounts can be saved without a shift.
*/

ALTER TABLE dbo.Employees
ALTER COLUMN ShiftId INT NULL;
