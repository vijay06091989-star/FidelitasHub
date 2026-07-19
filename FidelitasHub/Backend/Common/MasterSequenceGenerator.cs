using FidelitasHub.Data;

namespace FidelitasHub.Utilities
{
    public class MasterSequenceGenerator
    {
        private readonly ApplicationDbContext _context;

        public MasterSequenceGenerator(ApplicationDbContext context)
        {
            _context = context;
        }

        //====================================================
        // Department Code
        //====================================================

        public string GenerateDepartmentCode()
        {
            var lastDepartment = _context.Departments
                                         .OrderByDescending(d => d.DepartmentId)
                                         .FirstOrDefault();

            if (lastDepartment == null)
                return "DEP001";

            int number = int.Parse(lastDepartment.DepartmentCode.Substring(3));

            number++;

            return $"DEP{number:000}";
        }

        //====================================================
        // Shift Code
        //====================================================

        public string GenerateShiftCode()
        {
            var lastShift = _context.Shifts
                                    .OrderByDescending(s => s.ShiftId)
                                    .FirstOrDefault();

            if (lastShift == null)
                return "SH001";

            int number = int.Parse(lastShift.ShiftCode.Substring(2));

            number++;

            return $"SH{number:000}";
        }

        //====================================================
        // Employee Code
        // (Reserved for later)
        //====================================================

        public string GenerateEmployeeCode()
        {
            var lastEmployee = _context.Employees
                                       .OrderByDescending(e => e.EmployeeId)
                                       .FirstOrDefault();

            if (lastEmployee == null)
                return "EMP001";

            if (string.IsNullOrWhiteSpace(lastEmployee.EmployeeCode))
                return "EMP001";

            int number = int.Parse(lastEmployee.EmployeeCode.Substring(3));

            number++;

            return $"EMP{number:000}";
        }

        //====================================================
        // Designation Code
        // (Reserved for future)
        //====================================================

        public string GenerateDesignationCode()
        {
            // Will be used when we create Designation Master

            return "DES001";
        }
    }
}