using FidelitasHub.Data;
using FidelitasHub.Models;
using AttendanceModel = FidelitasHub.Models.Attendance;

namespace FidelitasHub.Services.Attendance
{
    public class AttendanceRegisterService : IAttendanceRegisterService
    {
        private readonly ApplicationDbContext _context;

        public AttendanceRegisterService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<AttendanceRegisterViewModel> GetAttendanceRegister(
    AttendanceRegisterRequest request)
        {
            //=============================
            // Validate Request
            //=============================

            if (!request.FromDate.HasValue)
            {
                request.FromDate = DateTime.Today;
            }

            if (!request.ToDate.HasValue)
            {
                request.ToDate = request.FromDate;
            }

            if (request.FromDate > request.ToDate)
            {
                return new List<AttendanceRegisterViewModel>();
            }

            if ((request.ToDate.Value - request.FromDate.Value).TotalDays > 90)
            {
                return new List<AttendanceRegisterViewModel>();
            }

            if (request.FromDate.Value < DateTime.Today.AddDays(-90))
            {
                return new List<AttendanceRegisterViewModel>();
            }

            //====================================
            // Get Active Employees
            //====================================

            var employees = GetActiveEmployees();

            //====================================
            // Load Shifts Once
            //====================================

            var shifts = _context.Shifts
                .Where(x => x.IsActive)
                .ToList();

            var shiftLookup = shifts.ToDictionary(
                x => x.ShiftId,
                x => x);

            //====================================
            // Get Dates
            //====================================

            var dates = GenerateDateList(
                request.FromDate.Value,
                request.ToDate.Value);

            //====================================
            // Load Attendance Records Once
            //====================================

            var attendanceRecords = _context.Attendances
                .Where(a =>
                    a.AttendanceDate >= request.FromDate.Value &&
                    a.AttendanceDate <= request.ToDate.Value)
                .ToList();

            //====================================
            // Attendance Lookup Dictionary
            //====================================

            var attendanceLookup = attendanceRecords
                .ToDictionary(
                    x => (x.EmployeeId, x.AttendanceDate.Date));

            //====================================
            // Result List
            //====================================

            var attendanceRegister = new List<AttendanceRegisterViewModel>();

            //====================================
            // Attendance Engine
            //====================================

            foreach (var employee in employees)
            {
                foreach (var attendanceDate in dates)
                {
                    attendanceLookup.TryGetValue(
    (employee.EmployeeId, attendanceDate.Date),
    out var attendance);

                    shiftLookup.TryGetValue(
    employee.ShiftId,
    out var shift);

                    attendanceRegister.Add(new AttendanceRegisterViewModel
                    {
                        EmployeeCode = employee.EmployeeCode,

                        EmployeeName = employee.EmployeeName,

                        Department = employee.Department,

                        Shift = shift?.ShiftName ?? "--",

                        AttendanceDate = attendanceDate,

                        PunchIn = attendance?.PunchIn?.ToString("hh:mm tt") ?? "--",

                        BreakStart = "--",

                        BreakEnd = "--",

                        PunchOut = attendance?.PunchOut?.ToString("hh:mm tt") ?? "--",

                        WorkedTime = attendance != null
    ? TimeSpan.FromMinutes(attendance.WorkedMinutes).ToString(@"hh\:mm")
    : "--",

                        BreakTime = attendance != null
    ? TimeSpan.FromMinutes(attendance.TotalBreakMinutes).ToString(@"hh\:mm")
    : "--",

                        Status = attendance?.Status ?? "Absent",

                        AttendanceStatus = CalculateAttendanceStatus(
    attendance,
    shift),

                        Remarks = ""
                    });
                }
            }


            //====================================
            // Shift Filter
            //====================================

            if (!string.IsNullOrWhiteSpace(request.Shift) &&
                request.Shift != "All")
            {
                attendanceRegister = attendanceRegister
                    .Where(x => x.Shift == request.Shift)
                    .ToList();
            }

            //====================================
            // Employee Filter
            //====================================

            if (!string.IsNullOrWhiteSpace(request.Employee))
            {
                var search = request.Employee.Trim();

                attendanceRegister = attendanceRegister
                    .Where(x =>
                        x.EmployeeCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        x.EmployeeName.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            //====================================
            // Status Filter
            //====================================

            if (!string.IsNullOrWhiteSpace(request.Status) &&
                request.Status != "All")
            {
                attendanceRegister = attendanceRegister
                    .Where(x => x.AttendanceStatus == request.Status)
                    .ToList();
            }

            return attendanceRegister;

        }

        //====================================================
        // ATTENDANCE STATUS POLICY
        //====================================================

        private string CalculateAttendanceStatus(
    AttendanceModel? attendance,
    Shift? shift)

        {
            if (attendance == null)
                return "Absent";

            //====================================
            // Today's Attendance - Live Status
            //====================================

            if (attendance.AttendanceDate.Date == DateTime.Today &&
                attendance.PunchOut == null)
            {
                return attendance.Status;
            }

            int workedMinutes = attendance.WorkedMinutes;

            if (workedMinutes < 240)
                return "Absent";

            if (workedMinutes >= 240 && workedMinutes <= 300)
                return "Half Day";

            if (workedMinutes > 300 && workedMinutes < 480)
                return "Present";

            if (workedMinutes >= 480)
            {
                if (attendance.PunchIn.HasValue && shift != null)
                {
                    var shiftStart = attendance.AttendanceDate.Date + shift.StandardStartTime;
                    var graceTime = shiftStart.AddMinutes(shift.GraceMinutes);

                    if (attendance.PunchIn.Value > graceTime)
                        return "Present - Late Entry";
                }

                return "Present";
            }

            return "Absent";
        }

        public List<Employee> GetActiveEmployees()
        {
            return _context.Employees
                .Where(x => x.IsActive)
                .OrderBy(x => x.EmployeeCode)
                .ToList();
        }

        public List<DateTime> GenerateDateList(
            DateTime fromDate,
            DateTime toDate)
        {
            var dates = new List<DateTime>();

            for (var date = fromDate.Date;
                 date <= toDate.Date;
                 date = date.AddDays(1))
            {
                dates.Add(date);
            }

            return dates;
        }
    }
}